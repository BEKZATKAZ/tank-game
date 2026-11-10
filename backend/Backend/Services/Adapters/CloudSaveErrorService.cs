using Microsoft.Extensions.Logging;
using System;
using Unity.Services.CloudCode.Shared;

namespace Backend.Services.Adapters;

public static class CloudSaveErrorService
{
    public static string GetCode(Exception exception, ILogger? logger, string operation)
    {
        int status = exception is ApiException api ? (int?)api.Response?.StatusCode ?? 0
            : 0;

        logger?.LogError(
            "Cloud Save {Operation}: status {Status}, type {Type}, stack {Stack}",
            operation, status, exception.GetType().Name, exception.StackTrace);

        return status switch
        {
            401 or 403 => "unauthorized",
            409 => "conflict",
            429 => "rate-limited",
            _ => "unknown"
        };
    }
}
