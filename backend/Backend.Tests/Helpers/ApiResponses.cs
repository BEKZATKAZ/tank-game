using Unity.Services.CloudCode.Shared;

namespace Backend.Tests.Helpers;

internal static class ApiResponses
{
    public static ApiResponse<T> Create<T>(T data)
    {
        ApiResponse<T> response = new();

        // The SDK exposes no public constructor or setter for response data.
        typeof(ApiResponse<T>).GetProperty(nameof(ApiResponse<>.Data))!
            .SetValue(response, data);

        return response;
    }
}
