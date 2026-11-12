using System;
using Unity.Mathematics;

namespace Tanks.Core.Players
{
    public sealed class PlayerLevelService : IPlayerLevelService
    {
        public int RequiredXP(int level)
        {
            int required = (int)(500 * math.pow(1.06f, level - 1));
            return (int)(required * 0.1f) * 10;
        }

        public PlayerLevel AddXP(PlayerLevel level, int xp)
        {
            if (xp < 0) throw new ArgumentOutOfRangeException(nameof(xp));
            long remaining = (long)level.XP + xp;

            while (remaining >= RequiredXP(level.Value))
            {
                int required = RequiredXP(level.Value);

                if (required <= 0)
                {
                    throw new InvalidOperationException("Invalid XP threshold");
                }

                remaining -= required;
                level.Value++;
            }

            level.XP = (int)remaining;
            return level;
        }
    }
}
