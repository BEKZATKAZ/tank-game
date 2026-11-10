using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Store;
using Unity.Services.CloudCode;
using UnityEngine;

namespace Tanks.Infrastructure
{
    public sealed class UGSCloudCodeCaller : ICloudCodeCaller
    {
        public async Task<ResultType<TResponse>> CallAsync<TPayload, TResponse>(
            TPayload payload)
        {
            string functionName = typeof(TPayload).Name;

            try
            {
                string json = await RequestAsync(payload, functionName);
                Debug.Log($"Module '{functionName}' returned: {json}");
                return CloudCodeResponseDecoder.Decode<TResponse>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Module '{functionName}' failed: {exception.GetType().Name}");
                return Result.Failure<TResponse>("unknown");
            }
        }

        public async Task<ResultType> CallAsync<TPayload>(TPayload payload)
        {
            string functionName = typeof(TPayload).Name;

            try
            {
                string json = await RequestAsync(payload, functionName);
                Debug.Log($"Module '{functionName}' returned: {json}");
                return CloudCodeResponseDecoder.Decode(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Module '{functionName}' failed: {exception.GetType().Name}");
                return Result.Failure("unknown");
            }
        }

        private static Task<string> RequestAsync<TPayload>(
            TPayload payload, string functionName)
        {
            string json = JsonConvert.SerializeObject(payload);

            Dictionary<string, object> parameters = JsonConvert
                .DeserializeObject<Dictionary<string, object>>(json);

            return CloudCodeService.Instance
                .CallModuleEndpointAsync("Backend", functionName, parameters);
        }
    }
}
