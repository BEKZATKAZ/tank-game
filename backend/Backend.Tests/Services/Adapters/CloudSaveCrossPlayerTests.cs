using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using NSubstitute;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Services.Adapters;

public sealed class CloudSaveCrossPlayerTests
{
    [Test]
    public async Task CrossPlayerRead_UsesServiceTokenRatherThanCallingPlayerToken()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveDataApi api = Substitute.For<ICloudSaveDataApi>();
        IGameApiClient client = Substitute.For<IGameApiClient>();

        context.PlayerId.Returns("host");
        context.ProjectId.Returns("project");
        context.AccessToken.Returns("player-token");
        context.ServiceToken.Returns("service-token");

        client.CloudSaveData.Returns(api);

        api.GetProtectedItemsAsync(context, "service-token", "project", "other",
            Arg.Any<List<string>>()).Returns(ApiResponses.Create(
                new GetItemsResponse([], new GetItemsResponseLinks(""))));

        CloudSaveService service = new(client);

        await service.LoadItemsAsync(context, "other", ["balance"]);

        await api.Received(1).GetProtectedItemsAsync(context, "service-token",
            "project",
            "other", Arg.Is<List<string>>(keys => keys.Single() == "balance"));
    }
}
