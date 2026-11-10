using System;

namespace Tanks.Core.Players
{
    public struct RewardClaimResult
    {
        public PlayerBalance Balance;
        public DateTimeOffset ServerUtc;
        public DateTimeOffset NextClaimAt;
    }
}
