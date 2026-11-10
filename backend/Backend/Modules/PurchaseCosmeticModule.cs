using Backend.Models;
using Backend.Models.Economy;
using Backend.Models.Store;
using Backend.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public sealed class PurchaseCosmeticModule(
    IRemoteConfigService remoteConfig,
    ICloudSaveService cloudSave)
{
    [CloudCodeFunction("PurchaseCosmetic")]
    public async Task<ApiResult<PlayerDTO>> Purchase(
        IExecutionContext context, string itemId, string currency, int? expectedPrice = null)
    {
        ApiResult result = await Handle(context, itemId, currency, expectedPrice);
        return result.IsFaulted ? ApiResult.Fail<PlayerDTO>(result.ErrorCode!)
            : await new GetLocalPlayerDataModule(cloudSave).Handle(context);
    }

    public async Task<ApiResult> Handle(
        IExecutionContext context, string itemId, string currency, int? expectedPrice = null)
    {
        ApiResult<CosmeticDefinition> itemResult = await remoteConfig
            .GetAsync<CosmeticDefinition>(context, "store.cosmetics", itemId);

        if (itemResult.IsFaulted)
        {
            return ApiResult.Fail(itemResult.ErrorCode!);
        }

        ApiResult<CloudItems> playerResult = await cloudSave
            .LoadItemsAsync(context, ["cosmetics", "balance"]);

        if (playerResult.IsFaulted)
        {
            return ApiResult.Fail(playerResult.ErrorCode!);
        }

        CloudItems player = playerResult.Value!;
        List<string> cosmetics = player.Get("cosmetics", fallback: new List<string>());

        if (itemId is "v3-body" or "v3-turret" || cosmetics.Contains(itemId))
        {
            return ApiResult.Fail("already-unlocked");
        }

        PlayerBalance balance = player.Get("balance", fallback: new PlayerBalance());
        Dictionary<string, int>? prices = itemResult.Value.Conditions.Prices;

        if (prices is null)
        {
            return ApiResult.Fail("unpurchasable-item");
        }

        if (prices.TryGetValue(currency, out int price) == false)
        {
            return ApiResult.Fail("invalid-data");
        }

        CurrencyType? currencyType = currency switch
        {
            "parts" or "spareParts" => CurrencyType.SpareParts,
            "pride" or "pridePoints" => CurrencyType.PridePoints,
            _ => null
        };

        if (currencyType.HasValue == false)
        {
            return ApiResult.Fail("invalid-data");
        }

        if (price < 0) return ApiResult.Fail("insufficient-funds");

        ApiResult<DiscountDefinition> discountResult = await remoteConfig
            .GetAsync<DiscountDefinition>(context, "store.discounted-items", itemId);

        if (discountResult.IsFaulted && discountResult.ErrorCode != "not-found")
        {
            return ApiResult.Fail(discountResult.ErrorCode!);
        }

        if (discountResult.IsSuccess)
        {
            DiscountDefinition discount = discountResult.Value;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if ((discount.EndDate == null || discount.EndDate > now) && discount.PercentOff is >= 0 and <= 100)
            {
                price = (int)(((long)price * (100 - discount.PercentOff) + 99) / 100);
            }
        }

        if (expectedPrice.HasValue && expectedPrice.Value != price)
        {
            return ApiResult.Fail("price-changed");
        }

        if (balance.TryConsume(currencyType.Value, price) == false)
        {
            return ApiResult.Fail("insufficient-funds");
        }

        cosmetics.Add(itemId);
        return await cloudSave.SaveItemsAsync(context, player);
    }
}
