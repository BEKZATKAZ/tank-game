using Backend.Models;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace Backend.Services.Adapters;

public interface IRemoteConfigService
{
    Task<ApiResult<T>> GetAsync<T>(
        IExecutionContext context, string key, string? subKey = null);
}

public sealed class RemoveConfigService(IGameApiClient client) : IRemoteConfigService
{
    public Task<ApiResult<T>> GetAsync<T>(
        IExecutionContext context, string key, string? subKey = null)
    {
        return ApiResultExtensions.RunAsync(async () =>
        {
            var result = await client.RemoteConfigSettings.AssignSettingsGetAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.EnvironmentId,
                null,
                [key]);

            if (result.Data.Configs.Settings.TryGetValue(key, out object? obj) == false)
            {
                return ApiResult.Fail<T>("not-found");
            }

            if (obj is null) return ApiResult.Fail<T>("invalid-data");

            JToken json = JToken.FromObject(obj);

            if (subKey != null)
            {
                JToken? sub = json[subKey];
                if (sub is null) return ApiResult.Fail<T>("not-found");
                json = sub;
            }

            if (json.Type == JTokenType.Null) return ApiResult.Fail<T>("invalid-data");

            T? deserialized = json.ToObject<T>();
            if (deserialized is null) return ApiResult.Fail<T>("invalid-data");

            return ApiResult.Ok(deserialized);
        });
    }
}
