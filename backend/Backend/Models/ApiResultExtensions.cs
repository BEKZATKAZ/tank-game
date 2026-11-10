using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Backend.Models;

public static class ApiResultExtensions
{
    public static async Task<ApiResult> RunAsync(
        Func<Task<ApiResult>> task,
        string errorCode = "unknown")
    {
        try { return await task(); }
        catch { return ApiResult.Fail(errorCode); }
    }

    public static async Task<ApiResult<T>> RunAsync<T>(
        Func<Task<ApiResult<T>>> task,
        string errorCode = "unknown")
    {
        try { return await task(); }
        catch { return ApiResult.Fail<T>(errorCode); }
    }
}
