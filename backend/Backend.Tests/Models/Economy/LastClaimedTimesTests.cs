using Backend.Models.Economy;
using Newtonsoft.Json;

namespace Backend.Tests.Models.Economy;

public class LastClaimedTimesTests
{
    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public void TryClaim_FirstClaim_GrantsRewardAndUpdatesOnlySelectedTimestamp(TimedRewardType type)
    {
        LastClaimedTimes times = new();
        PlayerBalance balance = new();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        bool claimed = times.TryClaim(type, balance, [new RewardClaim(CurrencyType.PridePoints, 50, 50)]);

        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.True);
            Assert.That(balance.PridePoints, Is.EqualTo(50));
            Assert.That(GetTime(times, type), Is.InRange(before, after));
            Assert.That(GetTime(times, OtherType(type)), Is.EqualTo(DateTimeOffset.MinValue));
        });
    }

    [TestCase(TimedRewardType.Playtime, 2.75, false)]
    [TestCase(TimedRewardType.Playtime, 3, true)]
    [TestCase(TimedRewardType.Playtime, 3.25, true)]
    [TestCase(TimedRewardType.Playtime, 12.25, true)]
    [TestCase(TimedRewardType.Playtime, 24.25, true)]
    [TestCase(TimedRewardType.Daily, 11.75, false)]
    [TestCase(TimedRewardType.Daily, 12, true)]
    [TestCase(TimedRewardType.Daily, 12.25, true)]
    [TestCase(TimedRewardType.Daily, 24.25, true)]
    [TestCase(TimedRewardType.Playtime, -1, false)]
    [TestCase(TimedRewardType.Daily, -1, false)]
    public void TryClaim_ElapsedTime_EnforcesFullCooldown(
        TimedRewardType type, double hoursAgo, bool expected)
    {
        DateTimeOffset previous = DateTimeOffset.UtcNow.AddHours(-hoursAgo);
        LastClaimedTimes times = CreateTimes(previous, previous);
        PlayerBalance balance = new();
        balance.Add(CurrencyType.PridePoints, 100);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        bool claimed = times.TryClaim(type, balance, [new RewardClaim(CurrencyType.PridePoints, 50, 50)]);

        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.EqualTo(expected));
            Assert.That(balance.PridePoints, Is.EqualTo(expected ? 150 : 100));
            Assert.That(GetTime(times, OtherType(type)), Is.EqualTo(previous));
            Assert.That(GetTime(times, type), expected
                ? Is.InRange(before, after)
                : Is.EqualTo(previous));
        });
    }

    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public void TryClaim_ImmediateRepeat_DoesNotGrantAgain(TimedRewardType type)
    {
        LastClaimedTimes times = CreateTimes(DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-3));
        PlayerBalance balance = new();
        RewardClaim[] rewards = [new RewardClaim(CurrencyType.PridePoints, 50, 50)];

        bool first = times.TryClaim(type, balance, rewards);
        DateTimeOffset claimedAt = GetTime(times, type);
        bool second = times.TryClaim(type, balance, rewards);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(balance.PridePoints, Is.EqualTo(50));
            Assert.That(GetTime(times, type), Is.EqualTo(claimedAt));
        });
    }

    [Test]
    public void TryClaim_DifferentRewardTypes_HaveIndependentCooldowns()
    {
        LastClaimedTimes times = new();
        PlayerBalance balance = new();
        RewardClaim[] rewards = [new RewardClaim(CurrencyType.PridePoints, 50, 50)];

        bool playtime = times.TryClaim(TimedRewardType.Playtime, balance, rewards);
        bool daily = times.TryClaim(TimedRewardType.Daily, balance, rewards);

        Assert.Multiple(() =>
        {
            Assert.That(playtime, Is.True);
            Assert.That(daily, Is.True);
            Assert.That(balance.PridePoints, Is.EqualTo(100));
        });
    }

    [Test]
    public void TryClaim_MultipleRewards_GrantsEveryReward()
    {
        LastClaimedTimes times = new();
        PlayerBalance balance = new();

        bool claimed = times.TryClaim(TimedRewardType.Daily, balance, [
            new RewardClaim(CurrencyType.PridePoints, 20, 20),
            new RewardClaim(CurrencyType.PridePoints, 30, 30)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.True);
            Assert.That(balance.PridePoints, Is.EqualTo(50));
        });
    }

    [Test]
    public void TryClaim_UnsupportedType_PreservesTimestampsAndBalance()
    {
        DateTimeOffset previous = DateTimeOffset.UtcNow.AddDays(-3);
        LastClaimedTimes times = CreateTimes(previous, previous);
        PlayerBalance balance = new();

        bool claimed = times.TryClaim((TimedRewardType)99, balance,
            [new RewardClaim(CurrencyType.PridePoints, 50, 50)]);

        Assert.Multiple(() =>
        {
            Assert.That(claimed, Is.False);
            Assert.That(balance.PridePoints, Is.Zero);
            Assert.That(times.Playtime, Is.EqualTo(previous));
            Assert.That(times.Daily, Is.EqualTo(previous));
        });
    }

    [Test]
    public void JsonRoundTrip_PreservesBothTimestamps()
    {
        DateTimeOffset playtime = new(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(5));
        DateTimeOffset daily = playtime.AddHours(-12);
        LastClaimedTimes original = CreateTimes(playtime, daily);

        string json = JsonConvert.SerializeObject(original);
        LastClaimedTimes restored = JsonConvert.DeserializeObject<LastClaimedTimes>(json)!;

        Assert.Multiple(() =>
        {
            Assert.That(restored.Playtime, Is.EqualTo(playtime));
            Assert.That(restored.Daily, Is.EqualTo(daily));
            Assert.That(restored.Playtime.Offset, Is.EqualTo(playtime.Offset));
            Assert.That(restored.Daily.Offset, Is.EqualTo(daily.Offset));
        });
    }

    private static LastClaimedTimes CreateTimes(DateTimeOffset playtime, DateTimeOffset daily)
    {
        string json = JsonConvert.SerializeObject(new { Playtime = playtime, Daily = daily });
        return JsonConvert.DeserializeObject<LastClaimedTimes>(json)!;
    }

    private static DateTimeOffset GetTime(LastClaimedTimes times, TimedRewardType type)
    {
        return type == TimedRewardType.Playtime ? times.Playtime : times.Daily;
    }

    private static TimedRewardType OtherType(TimedRewardType type)
    {
        return type == TimedRewardType.Playtime ? TimedRewardType.Daily : TimedRewardType.Playtime;
    }
}
