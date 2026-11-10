using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Backend.Models;

public readonly record struct ApiResult<T>(T? Value, string? ErrorCode)
{
    [JsonIgnore]
    public readonly bool IsSuccess => ErrorCode is null;

    [JsonIgnore]
    public readonly bool IsFaulted => ErrorCode is not null;
}
