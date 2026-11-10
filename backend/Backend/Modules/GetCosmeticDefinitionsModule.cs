using Backend.Models;
using Backend.Models.Store;
using Backend.Services.Adapters;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public readonly record struct CosmeticDefinitionsResponse(
    Dictionary<string, CosmeticDefinition> Items,
    Dictionary<string, DiscountDefinition> Discounts);

public sealed class GetCosmeticDefinitionsModule(IRemoteConfigService remoteConfig)
{
    [CloudCodeFunction("GetCosmeticDefinitions")]
    public async Task<ApiResult<CosmeticDefinitionsResponse>> Handle(IExecutionContext context)
    {
        var itemResults = await remoteConfig
            .GetAsync<Dictionary<string, CosmeticDefinition>>(context, "store.cosmetics");

        if (itemResults.IsFaulted)
        {
            return ApiResult.Fail<CosmeticDefinitionsResponse>(itemResults.ErrorCode!);
        }

        var discountResults = await remoteConfig
            .GetAsync<Dictionary<string, DiscountDefinition>>(context, "store.discounted-items");

        if (discountResults.IsFaulted)
        {
            return ApiResult.Fail<CosmeticDefinitionsResponse>(discountResults.ErrorCode!);
        }

        return ApiResult.Ok(new CosmeticDefinitionsResponse(
            itemResults.Value ?? [],
            discountResults.Value ?? []));
    }
}
