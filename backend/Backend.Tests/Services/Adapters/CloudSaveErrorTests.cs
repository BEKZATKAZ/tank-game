using Backend.Models;
using Backend.Services.Adapters;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Services.Adapters;

public sealed class CloudSaveErrorTests
{
    [TestCase(401, "unauthorized")]
    [TestCase(403, "unauthorized")]
    [TestCase(409, "conflict")]
    [TestCase(429, "rate-limited")]
    [TestCase(500, "unknown")]
    public async Task Save_MapsFailuresAndLogsWithoutCredentialsOrPayloads(
        int status,
        string code)
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        context.PlayerId.Returns("player");
        context.ProjectId.Returns("project");
        context.ServiceToken.Returns("service-secret");
        IApiResponse response = Substitute.For<IApiResponse>();
        response.StatusCode.Returns((HttpStatusCode)status);
        ApiException exception = new(ApiExceptionType.Http, "payload-secret", response);
        ICloudSaveDataApi api = Substitute.For<ICloudSaveDataApi>();
        api.SetProtectedItemBatchAsync(context, "service-secret", "project", "player",
            Arg.Any<SetItemBatchBody>()).Returns(
                Task.FromException<ApiResponse<SetItemBatchResponse>>(exception));

        IGameApiClient client = Substitute.For<IGameApiClient>();
        client.CloudSaveData.Returns(api);
        ILogger<CloudSaveService> logger = Substitute.For<ILogger<CloudSaveService>>();
        CloudSaveService service = new(client, logger);

        ApiResult result = await service.SaveItemsAsync(context, new CloudItems([]));

        Assert.That(result.ErrorCode, Is.EqualTo(code));
        string log = string.Join(" ", logger.ReceivedCalls().SelectMany(call =>
            call.GetArguments()).Select(value => value?.ToString()));

        Assert.That(log, Is.Not.Empty);
        Assert.That(log, Does.Not.Contain("service-secret"));
        Assert.That(log, Does.Not.Contain("payload-secret"));
    }

    [Test]
    public async Task MissingServiceToken_RejectsProtectedWriteBeforeApiCall()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        context.PlayerId.Returns("player");
        IGameApiClient client = Substitute.For<IGameApiClient>();
        CloudSaveService service = new(client);

        ApiResult result = await service.SaveItemsAsync(context, new CloudItems([]));

        Assert.That(result.ErrorCode, Is.EqualTo("unauthorized"));
        Assert.That(client.ReceivedCalls(), Is.Empty);
    }
}
