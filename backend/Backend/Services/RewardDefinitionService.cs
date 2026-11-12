using Backend.Models.Economy;
using System.Collections.Generic;

namespace Backend.Services;

public sealed class RewardDefinitionService
{
    private readonly Dictionary<TimedRewardType, RewardClaim[]> _rewards = new()
    {
        [TimedRewardType.Playtime] =
        [
            new RewardClaim(CurrencyType.SpareParts, 100, 300),
            new RewardClaim(CurrencyType.PridePoints, 10, 30)
        ],
        [TimedRewardType.Daily] =
        [
            new RewardClaim(CurrencyType.SpareParts, 500, 1000),
            new RewardClaim(CurrencyType.PridePoints, 40, 100)
        ],
    };

    public RewardClaim[] Get(TimedRewardType type) => _rewards[type];
}
