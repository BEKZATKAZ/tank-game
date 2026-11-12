using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Backend.Models.Economy;

public sealed class LastClaimedTimes
{
    [JsonProperty]
    public DateTimeOffset Playtime { get; private set; } = DateTimeOffset.MinValue;

    [JsonProperty]
    public DateTimeOffset Daily { get; private set; } = DateTimeOffset.MinValue;

    public DateTimeOffset GetDeadline(TimedRewardType type)
    {
        (DateTimeOffset last, int hours) = type switch
        {
            TimedRewardType.Playtime => (Playtime, 3),
            TimedRewardType.Daily => (Daily, 12),
            _ => (DateTimeOffset.MaxValue, 0)
        };

        return last > DateTimeOffset.MaxValue.AddHours(-hours)
            ? DateTimeOffset.MaxValue : last.AddHours(hours);
    }

    public bool TryClaim(
        TimedRewardType type, PlayerBalance balance, IEnumerable<RewardClaim> rewards, DateTimeOffset? utcNow = null)
    {
        (DateTimeOffset last, int desiredHours) = type switch
        {
            TimedRewardType.Playtime => (Playtime, 3),
            TimedRewardType.Daily => (Daily, 12),
            _ => (DateTimeOffset.MaxValue, int.MaxValue)
        };

        DateTimeOffset now = utcNow ?? DateTimeOffset.UtcNow;

        double elapsedHours = (now - last).TotalHours;
        if (elapsedHours < desiredHours) return false;

        _ = type switch
        {
            TimedRewardType.Playtime => Playtime = now,
            TimedRewardType.Daily => Daily = now,
            _ => default
        };

        foreach (RewardClaim reward in rewards) reward.Claim(balance);
        return true;
    }
}
