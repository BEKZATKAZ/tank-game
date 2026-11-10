using System;
using System.Collections.Generic;

namespace Tanks.Core.Players
{
    public struct PlayerSnapshot
    {
        public PlayerLevel Level;
        public PlayerBalance Balance;
        public List<string> Cosmetics;
        public EquippedCosmetics Equipped;
        public DateTimeOffset ServerUtc;
        public RewardDeadlines Rewards;
    }
}
