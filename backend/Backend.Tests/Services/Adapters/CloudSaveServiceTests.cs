using Backend.Models;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using NSubstitute;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Services.Adapters;

public class CloudSaveServiceTests
{
    private IExecutionContext _context = null!;
    private ICloudSaveDataApi _api = null!;
    private CloudSaveService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _context.PlayerId.Returns("player");
        _context.ProjectId.Returns("project");
        _context.AccessToken.Returns("token");
        _context.ServiceToken.Returns("service-token");

        _api = Substitute.For<ICloudSaveDataApi>();
        IGameApiClient client = Substitute.For<IGameApiClient>();
        client.CloudSaveData.Returns(_api);
        _service = new CloudSaveService(client);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task MissingPlayer_RejectsLoadAndSaveWithoutApiCalls(string? playerId)
    {
        _context.PlayerId.Returns(playerId);

        ApiResult<CloudItems> load = await _service.LoadItemsAsync(_context, "cosmetics");
        ApiResult save = await _service.SaveItemsAsync(_context, new CloudItems([]));

        Assert.Multiple(() =>
        {
            Assert.That(load.ErrorCode, Is.EqualTo("unauthorized"));
            Assert.That(save.ErrorCode, Is.EqualTo("unauthorized"));
            Assert.That(_api.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public async Task LoadItemsAsync_ForwardsContextAndKeysAndPreservesLocks()
    {
        _api.GetProtectedItemsAsync(_context, "token", "project", "player",
            Arg.Any<List<string>>())
            .Returns(ApiResponses.Create(new GetItemsResponse([
                CloudSaveItems.Create("cosmetics", new JArray("existing"), "lock")
            ], new GetItemsResponseLinks(""))));

        ApiResult<CloudItems> result = await _service.LoadItemsAsync(_context,
            "cosmetics", "balance");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Get<List<string>>("cosmetics"), Is.EqualTo(new[] {
                "existing" }));

            Assert.That(result.Value.GetSetters().Single().WriteLock, Is.EqualTo("lock"));
        });

        await _api.Received(1).GetProtectedItemsAsync(_context, "token", "project",
            "player",
            Arg.Is<List<string>>(keys => keys.SequenceEqual(new[] { "cosmetics",
                "balance" })));
    }

    [Test]
    public async Task LoadItemsAsync_EmptyResponse_ProducesEmptyCloudItems()
    {
        _api.GetProtectedItemsAsync(_context, "token", "project", "player",
            Arg.Any<List<string>>())
            .Returns(ApiResponses.Create(new GetItemsResponse([], new
                GetItemsResponseLinks(""))));

        ApiResult<CloudItems> result = await _service.LoadItemsAsync(_context, "cosmetics");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.GetSetters(), Is.Empty);
        });
    }

    [Test]
    public async Task SaveItemsAsync_ForwardsChangedValuesAndWriteLocks()
    {
        CloudItems items = new([CloudSaveItems.Create("cosmetics", new
            JArray("existing"), "lock")]);

        items.Get<List<string>>("cosmetics").Add("new");
        _api.SetProtectedItemBatchAsync(_context, "service-token", "project", "player",
            Arg.Any<SetItemBatchBody>())
            .Returns(new ApiResponse<SetItemBatchResponse>());

        ApiResult result = await _service.SaveItemsAsync(_context, items);

        Assert.That(result.IsSuccess, Is.True);
        await _api.Received(1).SetProtectedItemBatchAsync(_context, "service-token",
            "project", "player",
            Arg.Is<SetItemBatchBody>(body => body.Data.Count == 1
                && body.Data[0].Key == "cosmetics"
                && body.Data[0].WriteLock == "lock"
                && ((List<string>)body.Data[0].Value).SequenceEqual(new[] { "existing",
                    "new" })));
    }

    [Test]
    public async Task LoadItemsAsync_ApiException_ReturnsUnknown()
    {
        _api.GetProtectedItemsAsync(_context, "token", "project", "player",
            Arg.Any<List<string>>())
            .Returns(Task.FromException<ApiResponse<GetItemsResponse>>(new
                InvalidOperationException()));

        ApiResult<CloudItems> result = await _service.LoadItemsAsync(_context, "cosmetics");

        Assert.That(result.ErrorCode, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task SaveItemsAsync_ApiException_ReturnsUnknownWithoutRetry()
    {
        _api.SetProtectedItemBatchAsync(_context, "service-token", "project", "player",
            Arg.Any<SetItemBatchBody>())
            .Returns(Task.FromException<ApiResponse<SetItemBatchResponse>>(new
                InvalidOperationException()));

        ApiResult result = await _service.SaveItemsAsync(_context, new CloudItems([]));

        Assert.That(result.ErrorCode, Is.EqualTo("unknown"));
        await _api.Received(1).SetProtectedItemBatchAsync(
            _context, "service-token", "project", "player", Arg.Any<SetItemBatchBody>());
    }
}
