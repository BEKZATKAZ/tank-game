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

public sealed class ClaimRewardModuleTests : ClaimRewardTestsBase
{
    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public async Task Claim_Eligible_GrantsConfiguredRewardAndSavesOnce(
        TimedRewardType type)
    {
        (int minimumParts, int maximumParts) = GetRange(type, CurrencyType.SpareParts);
        (int minimumPoints, int maximumPoints) = GetRange(type, CurrencyType.PridePoints);

        DateTimeOffset previous = ((JObject)_items.Single(item => item.Key ==
            "lastClaimedTimes").Value)
            .Value<DateTimeOffset>("Playtime");

        DateTimeOffset before = DateTimeOffset.UtcNow;

        ApiResult<RewardClaimResult> result = await Claim(type);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        LastClaimedTimes times = _loaded.Get<LastClaimedTimes>("lastClaimedTimes");
        List<SetItemBody> setters = _loaded.GetSetters();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_loaded.Get<PlayerBalance>("balance").PridePoints,
                Is.InRange(100 + minimumPoints, 100 + maximumPoints));

            Assert.That(_loaded.Get<PlayerBalance>("balance").SpareParts,
                Is.InRange(100 + minimumParts, 100 + maximumParts));

            Assert.That(GetTime(times, type), Is.InRange(before, after));
            Assert.That(GetTime(times, OtherType(type)), Is.EqualTo(previous));
            Assert.That(setters, Has.Count.EqualTo(2));
            Assert.That(setters.Single(setter => setter.Key ==
                "lastClaimedTimes").WriteLock,
                Is.EqualTo("times-lock"));

            Assert.That(setters.Single(setter => setter.Key == "balance").WriteLock,
                Is.EqualTo("balance-lock"));
        });

        await _cloudSave.Received(1).LoadItemsAsync(_context,
            Arg.Is<List<string>>(keys => keys.SequenceEqual(new[] { "lastClaimedTimes",
                "balance" })));

        await _cloudSave.Received(1).SaveItemsAsync(_context, _loaded);
    }

    [TestCase(TimedRewardType.Playtime, true, false)]
    [TestCase(TimedRewardType.Playtime, false, true)]
    [TestCase(TimedRewardType.Playtime, true, true)]
    [TestCase(TimedRewardType.Daily, true, false)]
    [TestCase(TimedRewardType.Daily, false, true)]
    [TestCase(TimedRewardType.Daily, true, true)]
    public async Task Claim_MissingKeys_UsesFallbacksAndCreatesSettersWithoutWriteLocks(
        TimedRewardType type,
        bool missingTimes,
        bool missingBalance)
    {
        (int minimumParts, int maximumParts) = GetRange(type, CurrencyType.SpareParts);
        (int minimumPoints, int maximumPoints) = GetRange(type, CurrencyType.PridePoints);

        _items.RemoveAll(item => (missingTimes && item.Key == "lastClaimedTimes")
            || (missingBalance && item.Key == "balance"));

        ApiResult<RewardClaimResult> result = await Claim(type);

        int initial = missingBalance ? 0 : 100;
        List<SetItemBody> setters = _loaded.GetSetters();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_loaded.Get<PlayerBalance>("balance").PridePoints,
                Is.InRange(initial + minimumPoints, initial + maximumPoints));

            Assert.That(_loaded.Get<PlayerBalance>("balance").SpareParts,
                Is.InRange(initial + minimumParts, initial + maximumParts));

            Assert.That(setters, Has.Count.EqualTo(2));
            Assert.That(setters.Single(setter => setter.Key ==
                "lastClaimedTimes").WriteLock,
                Is.EqualTo(missingTimes ? null : "times-lock"));

            Assert.That(setters.Single(setter => setter.Key == "balance").WriteLock,
                Is.EqualTo(missingBalance ? null : "balance-lock"));
        });

        await _cloudSave.Received(1).SaveItemsAsync(_context, _loaded);
    }

    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public async Task Claim_PersistedTimestamp_PreventsDuplicateReward(
        TimedRewardType type)
    {
        ApiResult<RewardClaimResult> first = await Claim(type);
        int balanceAfterFirstClaim = _loaded.Get<PlayerBalance>("balance").PridePoints;
        int partsAfterFirstClaim = _loaded.Get<PlayerBalance>("balance").SpareParts;

        DateTimeOffset claimedAt =
            GetTime(_loaded.Get<LastClaimedTimes>("lastClaimedTimes"), type);

        ApiResult<RewardClaimResult> second = await Claim(type);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(second.ErrorCode, Is.EqualTo("already-claimed"));
            Assert.That(_loaded.Get<PlayerBalance>("balance").PridePoints,
                Is.EqualTo(balanceAfterFirstClaim));

            Assert.That(_loaded.Get<PlayerBalance>("balance").SpareParts,
                Is.EqualTo(partsAfterFirstClaim));

            Assert.That(GetTime(_loaded.Get<LastClaimedTimes>("lastClaimedTimes"),
                type), Is.EqualTo(claimedAt));
        });

        await _cloudSave.Received(1).SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [Test]
    public async Task Claim_BothTypes_GrantsIndependentRewards()
    {
        (int partsMin, int partsMax) = GetCombinedRange(CurrencyType.SpareParts);
        (int prideMin, int prideMax) = GetCombinedRange(CurrencyType.PridePoints);

        ApiResult<RewardClaimResult> playtime = await Claim(TimedRewardType.Playtime);
        ApiResult<RewardClaimResult> daily = await Claim(TimedRewardType.Daily);

        Assert.Multiple(() =>
        {
            Assert.That(playtime.IsSuccess, Is.True);
            Assert.That(daily.IsSuccess, Is.True);
            Assert.That(_loaded.Get<PlayerBalance>("balance").PridePoints,
                Is.InRange(100 + prideMin, 100 + prideMax));

            Assert.That(_loaded.Get<PlayerBalance>("balance").SpareParts, Is.InRange(100
                + partsMin, 100 + partsMax));
        });

        await _cloudSave.Received(2).SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

}
