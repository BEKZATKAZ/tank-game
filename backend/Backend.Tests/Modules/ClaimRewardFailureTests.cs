using Backend.Models;
using Backend.Models.Economy;
using Backend.Modules;
using Backend.Services;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using NSubstitute;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Modules;

public sealed class ClaimRewardFailureTests : ClaimRewardTestsBase
{
    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public async Task Claim_LoadFailure_PropagatesErrorAndSkipsSave(
        TimedRewardType type)
    {
        _cloudSave.LoadItemsAsync(_context, Arg.Any<List<string>>())
            .Returns(ApiResult.Fail<CloudItems>("unauthorized"));

        ApiResult<RewardClaimResult> result = await Claim(type);

        Assert.That(result.ErrorCode, Is.EqualTo("unauthorized"));

        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public async Task Claim_DuringCooldown_PreservesPlayerAndSkipsSave(
        TimedRewardType type)
    {
        DateTimeOffset previous = DateTimeOffset.UtcNow.AddMinutes(-30);
        _items = CreateItems(previous);

        ApiResult<RewardClaimResult> result = await Claim(type);

        LastClaimedTimes times = _loaded.Get<LastClaimedTimes>("lastClaimedTimes");

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo("already-claimed"));
            Assert.That(_loaded.Get<PlayerBalance>("balance").PridePoints, Is.EqualTo(100));
            Assert.That(_loaded.Get<PlayerBalance>("balance").SpareParts, Is.EqualTo(100));
            Assert.That(times.Playtime, Is.EqualTo(previous));
            Assert.That(times.Daily, Is.EqualTo(previous));
        });

        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public async Task Claim_SaveFailure_PropagatesErrorWithoutRetry(
        TimedRewardType type)
    {
        _cloudSave.SaveItemsAsync(_context, Arg.Any<CloudItems>())
            .Returns(ApiResult.Fail("conflict"));

        ApiResult<RewardClaimResult> result = await Claim(type);

        Assert.That(result.ErrorCode, Is.EqualTo("conflict"));

        await _cloudSave.Received(1).SaveItemsAsync(_context, _loaded);
    }

}
