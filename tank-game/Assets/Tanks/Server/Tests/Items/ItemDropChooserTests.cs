using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Pickups;
using Tanks.Shared.Items.Data;

namespace Tanks.Server.Tests.Items
{
    public sealed class ItemDropChooserTests
    {
        [Test]
        public void Choose_UsesEqualTierChancesRegardlessOfItemCounts()
        {
            IItemConfig[] items = Enumerable.Range(0, 34).Select(index =>
            {
                IItemConfig item = Substitute.For<IItemConfig>();
                item.Tier.Returns(index < 30 ? ItemTier.Rare : (ItemTier)(index - 29));
                return item;
            }).ToArray();

            Random random = new(12345);
            int[] counts = new int[5];

            for (int i = 0; i < 9000; i++)
            {
                counts[(int)ItemDropChooser.Choose(items, random).Tier]++;
            }

            Assert.That(counts[(int)ItemTier.Uncommon], Is.Zero);
            Assert.That(counts[(int)ItemTier.Legendary], Is.Zero);
            Assert.That(counts[(int)ItemTier.Rare], Is.InRange(2800, 3200));
            Assert.That(counts[(int)ItemTier.Epic], Is.InRange(2800, 3200));
            Assert.That(counts[(int)ItemTier.Unstable], Is.InRange(2800, 3200));
        }

        [Test]
        public void Choose_NoEligibleItems_ReturnsNull()
        {
            IItemConfig item = Substitute.For<IItemConfig>();
            item.Tier.Returns(ItemTier.Legendary);

            Assert.That(ItemDropChooser.Choose(new[] { item }, new Random(1)), Is.Null);
        }
    }
}
