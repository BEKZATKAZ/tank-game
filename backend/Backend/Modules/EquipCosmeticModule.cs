using Backend.Models;
using Backend.Models.Store;
using Backend.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public sealed class EquipCosmeticModule(ICloudSaveService cloudSave)
{
    [CloudCodeFunction("EquipCosmetic")]
    public async Task<ApiResult<PlayerDTO>> Handle(IExecutionContext context, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return ApiResult.Fail<PlayerDTO>("invalid-data");
        }

        bool isBody = itemId.EndsWith("-body", StringComparison.Ordinal);
        bool isTurret = itemId.EndsWith("-turret", StringComparison.Ordinal);

        if (isBody == false && isTurret == false)
        {
            return ApiResult.Fail<PlayerDTO>("invalid-data");
        }

        bool isDefault = itemId is "v3-body" or "v3-turret";
        var result = await cloudSave.LoadItemsAsync(context, ["cosmetics", "equippedCosmetics"]);

        if (result.IsFaulted)
        {
            return ApiResult.Fail<PlayerDTO>(result.ErrorCode!);
        }

        CloudItems player = result.Value!;
        var cosmetics = player.Get("cosmetics", fallback: new List<string>());

        if (isDefault == false && cosmetics.Contains(itemId) == false)
        {
            return ApiResult.Fail<PlayerDTO>("not-unlocked");
        }

        var equipped = player.Get("equippedCosmetics", fallback: EquippedCosmetics.Default);

        player.Set("equippedCosmetics", new EquippedCosmetics(
            isBody ? itemId : equipped.Body, isTurret ? itemId : equipped.Turret));

        ApiResult saved = await cloudSave.SaveItemsAsync(context, player);

        if (saved.IsFaulted)
        {
            return ApiResult.Fail<PlayerDTO>(saved.ErrorCode!);
        }

        return await new GetLocalPlayerDataModule(cloudSave).Handle(context);
    }
}
