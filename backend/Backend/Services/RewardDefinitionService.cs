using Backend.Models.Economy;
using System.Collections.Generic;

namespace Backend.Services;

public sealed class RewardDefinitionService
{
    private readonly Dictionary<TimedRewardType, RewardClaim[]> _rewards = new()
    {
        [TimedRewardType.Playtime] =
        [
            new RewardClaim(CurrencyType.SpareParts, 150, 400),
            new RewardClaim(CurrencyType.PridePoints, 20, 40)
        ],
        [TimedRewardType.Daily] =
        [
            new RewardClaim(CurrencyType.SpareParts, 700, 1500),
            new RewardClaim(CurrencyType.PridePoints, 80, 200)
        ],
    };

    public RewardClaim[] Get(TimedRewardType type) => _rewards[type];
}
