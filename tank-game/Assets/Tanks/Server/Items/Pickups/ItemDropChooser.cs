using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Items.Data;

namespace Tanks.Server.Items.Pickups
{
    public static class ItemDropChooser
    {
        public static IItemConfig Choose(IEnumerable<IItemConfig> items, Random random)
        {
            IItemConfig[][] tiers = items
                .Where(x => x.Tier is ItemTier.Rare or ItemTier.Epic or ItemTier.Unstable)
                .GroupBy(x => x.Tier)
                .Select(x => x.ToArray())
                .ToArray();

            if (tiers.Length == 0) return null;
            IItemConfig[] tier = tiers[random.Next(tiers.Length)];
            return tier[random.Next(tier.Length)];
        }
    }
}
