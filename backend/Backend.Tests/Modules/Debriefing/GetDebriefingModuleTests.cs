using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Models.Economy;
using Backend.Models.Leveling;
using Backend.Modules;
using Backend.Services;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Modules.Debriefing;

public sealed class GetDebriefingModuleTests
{
    private IExecutionContext _context = null!;
    private ICloudSaveService _save = null!;
    private GetDebriefingModule _module = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _save = Substitute.For<ICloudSaveService>();

        _save.LoadItemsAsync(_context, Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(_ => ApiResult.Ok(new CloudItems([])));

        _save.SaveItemsAsync(_context, Arg.Any<string>(), Arg.Any<CloudItems>())
            .Returns(ApiResult.Ok());

        _module = new GetDebriefingModule(new PlayerDebriefingService(
            _save, new DebriefingCalculationService()));
    }

    [Test]
    public async Task Rewards_SaveProgressAndReceiptTogether_AndReturnAppliedRows()
    {
        PlayerDebriefingPayload player = new("player", 2, 3, 4, 5, true);

        JObject storedLevel = JObject.FromObject(new { Value = 1, XP = 0 });

        JObject storedBalance = JObject.FromObject(new
        {
            SpareParts = 0, PridePoints = 3
        });

        CloudItems items = new([
            CloudSaveItems.Create("level", storedLevel, "level-lock"),
            CloudSaveItems.Create("balance", storedBalance, "balance-lock")
        ]);

        _save.LoadItemsAsync(_context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(items));

        ApiResult<DebriefingResponse> response = await _module.Handle(_context,
            "match", [player]);

        PlayerDebriefing result = response.Value.Conclusions.Single().Value!;

        Level expected = new();

        expected.AddXP(result.Rows.Sum(row => row.XP));

        Assert.Multiple(() =>
        {
            Assert.That(result.Rows[0], Is.EqualTo(
                DebriefingCalculationService.EliminationReward.Calculate(2)));

            Assert.That(result.Rows[1], Is.EqualTo(
                DebriefingCalculationService.AssistReward.Calculate(3)));

            Assert.That(result.Rows[2].PridePoints, Is.EqualTo(-3));

            Assert.That(result.Rows[3], Is.EqualTo(
                DebriefingCalculationService.ScoreReward.Calculate(5)));

            Assert.That(result.Rows[4], Is.EqualTo(
                DebriefingCalculationService.VictoryReward.Calculate(1)));

            Assert.That(result.AfterLevel, Is.EqualTo(new DebriefingLevel(
                expected.Value, expected.XP)));

            Assert.That(result.AfterBalance.PridePoints, Is.EqualTo(
                result.BeforeBalance.PridePoints + result.Rows.Sum(
                    row => row.PridePoints)));

            Assert.That(items.GetSetters().Select(item => item.Key), Is.EquivalentTo(
                new[] { "level", "balance", "debriefingReceipt" }));

            Assert.That(
                items.GetSetters().Single(item => item.Key == "level").WriteLock,
                Is.EqualTo("level-lock"));
        });

        await _save.Received(1).SaveItemsAsync(_context, "player", items);
    }

    [Test]
    public async Task Replay_ReturnsReceiptWithoutSavingAgain()
    {
        PlayerDebriefingPayload player = new("player", 1, 0, 0, 0, false);
        CloudItems items = new([]);

        _save.LoadItemsAsync(_context, "player", Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(items));

        ApiResult<DebriefingResponse> first = await _module.Handle(_context, "match",
            [player]);

        ApiResult<DebriefingResponse> replay = await _module.Handle(_context,
            "match", [player]);

        Assert.That(replay.Value.Conclusions[0].Value,
            Is.EqualTo(first.Value.Conclusions[0].Value));

        await _save.Received(1).SaveItemsAsync(_context, "player", items);
    }

    [TestCase("conflict")]
    [TestCase("unauthorized")]
    [TestCase("rate-limited")]
    public async Task SaveFailure_IsCorrelatedWithoutHidingOtherSuccess(string error)
    {
        _save.SaveItemsAsync(_context, "bad", Arg.Any<CloudItems>())
            .Returns(ApiResult.Fail(error));

        ApiResult<DebriefingResponse> result = await _module.Handle(_context,
            "match",
            [new("bad", 0, 0, 0, 0, false), new("good", 0, 0, 0, 0, false)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Conclusions[0].ErrorCode, Is.EqualTo(error));
            Assert.That(result.Value.Conclusions[0].Value, Is.Null);
            Assert.That(result.Value.Conclusions[1].ErrorCode, Is.Null);
        });
    }

    [Test]
    public async Task InvalidBatch_DoesNotWriteAnyPlayer()
    {
        ApiResult<DebriefingResponse> result = await _module.Handle(_context,
            "match",
            [new("same", 0, 0, 0, 0, false), new("same", 0, 0, 0, 0, false)]);

        Assert.That(result.ErrorCode, Is.EqualTo("invalid-payload"));
        Assert.That(_save.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task Overflow_IsAnExplicitPlayerFailure()
    {
        ApiResult<DebriefingResponse> result = await _module.Handle(_context,
            "match",
            [new("player", int.MaxValue, 0, 0, 0, false)]);

        Assert.That(result.Value.Conclusions[0].ErrorCode,
            Is.EqualTo("invalid-payload"));

        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<string>(),
            Arg.Any<CloudItems>());
    }
}
