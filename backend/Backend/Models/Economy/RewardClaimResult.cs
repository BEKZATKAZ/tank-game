using System;

namespace Backend.Models.Economy;

public readonly record struct RewardClaimResult(
    PlayerBalance Balance,
    DateTimeOffset ServerUtc,
    DateTimeOffset NextClaimAt);
