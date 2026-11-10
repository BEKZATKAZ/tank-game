using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Backend.Models;

public readonly record struct ApiResult(string? ErrorCode)
{
    [JsonIgnore]
    public readonly bool IsSuccess => ErrorCode is null;

    [JsonIgnore]
    public readonly bool IsFaulted => ErrorCode is not null;

    public static ApiResult Ok()
    {
        return new ApiResult(null);
    }

    public static ApiResult Fail(string errorCode)
    {
        return new ApiResult(errorCode);
    }

    public static ApiResult<T> Ok<T>(T value)
    {
        return new ApiResult<T>(value, null);
    }

    public static ApiResult<T> Fail<T>(string errorCode)
    {
        return new ApiResult<T>(default, errorCode);
    }
}
