using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Services;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Services;

public sealed class PlayerDebriefingFailureTests
{
    [Test]
    public async Task SerializedReceipt_ReplaysWithoutAnotherSave()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(new CloudItems([])));

        save.SaveItemsAsync(context, "player", Arg.Any<CloudItems>())
            .Returns(ApiResult.Ok());

        PlayerDebriefingService service = new(save,
            new DebriefingCalculationService());

        PlayerDebriefingPayload payload = new("player", 1, 2, 1, 3, true);

        ApiResult<PlayerDebriefing> first = await service.CalculateAsync(context,
            "match", payload);

        string serialized = JsonConvert.SerializeObject(first.Value);

        CloudItems persisted = new([
            CloudSaveItems.Create("debriefingReceipt", JObject.Parse(serialized),
                "receipt-lock")
        ]);

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(persisted));

        ApiResult<PlayerDebriefing> replay = await service.CalculateAsync(context,
            "match", payload);

        Assert.Multiple(() =>
        {
            Assert.That(replay.IsSuccess, Is.True);

            Assert.That(JsonConvert.SerializeObject(replay.Value),
                Is.EqualTo(serialized));
        });

        await save.Received(1).SaveItemsAsync(context, "player",
            Arg.Any<CloudItems>());
    }

    [TestCase("unauthorized")]
    [TestCase("unknown")]
    public async Task LoadFailure_PreservesErrorWithoutSaving(string error)
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Fail<CloudItems>(error));

        PlayerDebriefingService service = new(save,
            new DebriefingCalculationService());

        ApiResult<PlayerDebriefing> result = await service.CalculateAsync(context,
            "match", new("player", 1, 1, 1, 1, true));

        Assert.That(result.ErrorCode, Is.EqualTo(error));

        await save.DidNotReceive().SaveItemsAsync(context, Arg.Any<string>(),
            Arg.Any<CloudItems>());
    }

    [Test]
    public async Task ConflictRetry_RecalculatesFromFreshStateAndWriteLocks()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        CloudItems first = new([]);

        CloudItems fresh = new([
            CloudSaveItems.Create("balance", JObject.FromObject(new
                { SpareParts = 700, PridePoints = 500 }), "new-lock")
        ]);

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(first), ApiResult.Ok(fresh));

        save.SaveItemsAsync(context, "player", Arg.Any<CloudItems>())
            .Returns(ApiResult.Fail("conflict"), ApiResult.Ok());

        PlayerDebriefingService service = new(save,
            new DebriefingCalculationService());

        PlayerDebriefingPayload payload = new("player", 1, 0, 0, 0, false);

        ApiResult<PlayerDebriefing> failed = await service.CalculateAsync(context,
            "match", payload);

        ApiResult<PlayerDebriefing> success = await service.CalculateAsync(context,
            "match", payload);

        Assert.Multiple(() =>
        {
            Assert.That(failed.ErrorCode, Is.EqualTo("conflict"));
            Assert.That(success.IsSuccess, Is.True);
            Assert.That(success.Value!.BeforeBalance.SpareParts, Is.EqualTo(700));

            Assert.That(success.Value.AfterBalance.SpareParts, Is.EqualTo(
                700 + DebriefingCalculationService.EliminationReward.SpareParts));

            Assert.That(
                fresh.GetSetters().Single(item => item.Key == "balance").WriteLock,
                Is.EqualTo("new-lock"));
        });
    }

    [Test]
    public async Task UnexpectedException_IsAnExplicitFailure()
    {
        IExecutionContext context = Substitute.For<IExecutionContext>();
        ICloudSaveService save = Substitute.For<ICloudSaveService>();

        save.LoadItemsAsync(context, "player", Arg.Any<List<string>>())
            .Returns(
                Task.FromException<ApiResult<CloudItems>>(
                    new InvalidOperationException()));

        PlayerDebriefingService service = new(save,
            new DebriefingCalculationService());

        ApiResult<PlayerDebriefing> result = await service.CalculateAsync(context,
            "match", new("player", 1, 0, 0, 0, false));

        Assert.That(result.ErrorCode, Is.EqualTo("unknown"));
    }
}
