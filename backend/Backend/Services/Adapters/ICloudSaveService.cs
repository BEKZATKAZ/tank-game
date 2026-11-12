using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Services.Adapters;

public interface ICloudSaveService
{
    Task<ApiResult<CloudItems>> LoadItemsAsync(
        IExecutionContext context,
        List<string> keys);

    Task<ApiResult> SaveItemsAsync(IExecutionContext context, CloudItems value);

    Task<ApiResult<CloudItems>> LoadItemsAsync(
        IExecutionContext context,
        string playerId,
        List<string> keys);

    Task<ApiResult> SaveItemsAsync(
        IExecutionContext context,
        string playerId,
        CloudItems value);
}
