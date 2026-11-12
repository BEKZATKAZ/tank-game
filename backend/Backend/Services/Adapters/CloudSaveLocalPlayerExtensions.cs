using Backend.Models;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Services.Adapters;

public static class CloudSaveLocalPlayerExtensions
{
    public static Task<ApiResult<CloudItems>> LoadItemsAsync(
        this ICloudSaveService service,
        IExecutionContext context,
        params string[] keys)
    {
        return service.LoadItemsAsync(context, [.. keys]);
    }
}
