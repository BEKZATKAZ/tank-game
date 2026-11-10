using Backend.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Backend.Services.Adapters;

public sealed class CloudSaveService : ICloudSaveService
{
    public CloudSaveService(
        IGameApiClient client,
        ILogger<CloudSaveService>? logger = null)
    {
        _client = client;
        _logger = logger;
    }

    private readonly IGameApiClient _client;
    private readonly ILogger<CloudSaveService>? _logger;

    public async Task<ApiResult<CloudItems>> LoadItemsAsync(
        IExecutionContext context,
        params List<string> keys)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId))
        {
            return ApiResult.Fail<CloudItems>("unauthorized");
        }

        try
        {
            ApiResponse<GetItemsResponse> response = await
                _client.CloudSaveData.GetProtectedItemsAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId, keys);

            return ApiResult.Ok(new CloudItems(response.Data.Results));
        }
        catch (Exception exception)
        {
            string code = CloudSaveErrorService.GetCode(exception, _logger, "load");
            return ApiResult.Fail<CloudItems>(code);
        }
    }

    public async Task<ApiResult> SaveItemsAsync(
        IExecutionContext context,
        CloudItems value)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId) ||
            string.IsNullOrWhiteSpace(context.ServiceToken))
        {
            return ApiResult.Fail("unauthorized");
        }

        try
        {
            await _client.CloudSaveData.SetProtectedItemBatchAsync(
                context, context.ServiceToken, context.ProjectId, context.PlayerId,
                new SetItemBatchBody(value.GetSetters()));

            return ApiResult.Ok();
        }
        catch (Exception exception)
        {
            return ApiResult.Fail(CloudSaveErrorService.GetCode(exception, _logger,
                "save"));
        }
    }
}
