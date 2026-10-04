using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Core.Helpers;
using Tanks.Server.Items.Pickups;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Server.Tests.Items
{
    class MockItemConfig : IItemConfig
    {
        public ItemIdentifier Identifier { get; set; }
        public ItemTier Tier { get; set; }

        public IReadOnlyDictionary<StatType, PlayerStat> PlayerStats => PlayerStatsDictionary;
        public IReadOnlyDictionary<string, ItemStat> ItemStats => ItemStatsDictionary;
        public IEnumerable<ItemActionConfigBase> OnPickUpActions { get; set; }

        public Dictionary<StatType, PlayerStat> PlayerStatsDictionary { get; set; } = new();
        public Dictionary<string, ItemStat> ItemStatsDictionary { get; set; } = new();

        public ItemStat GetStat(string name)
        {
            return ItemStatsDictionary[name];
        }
    }

    public class ItemChooserTests
    {
        private static readonly Dictionary<ItemTier, PositiveFloat> _chanceRatios = new()
        {
            [ItemTier.Uncommon] = 50,
            [ItemTier.Rare] = 30,
            [ItemTier.Epic] = 12,
            [ItemTier.Unstable] = 6,
            [ItemTier.Legendary] = 2
        };

        private static readonly IItemConfig[] _items =
        {
            new MockItemConfig { Identifier = new("item1_uncommon"), Tier = ItemTier.Uncommon },
            new MockItemConfig { Identifier = new("item2_uncommon"), Tier = ItemTier.Uncommon },
            new MockItemConfig { Identifier = new("item3_uncommon"), Tier = ItemTier.Uncommon },
            new MockItemConfig { Identifier = new("item4_uncommon"), Tier = ItemTier.Uncommon },

            new MockItemConfig { Identifier = new("item5_rare"), Tier = ItemTier.Rare },
            new MockItemConfig { Identifier = new("item6_rare"), Tier = ItemTier.Rare },
            new MockItemConfig { Identifier = new("item7_rare"), Tier = ItemTier.Rare },

            new MockItemConfig { Identifier = new("item8_epic"), Tier = ItemTier.Epic },
            new MockItemConfig { Identifier = new("item9_epic"), Tier = ItemTier.Epic },

            new MockItemConfig { Identifier = new("item10_unstable"), Tier = ItemTier.Unstable },
            new MockItemConfig { Identifier = new("item11_unstable"), Tier = ItemTier.Unstable },

            new MockItemConfig { Identifier = new("item12_legendary"), Tier = ItemTier.Legendary }
        };

        private ItemChooser _chooser;

        [SetUp]
        public void Setup()
        {
            _chooser = new ItemChooser(_items, _chanceRatios, seed: 123);
        }

        [TestCase(0.0f, ItemTier.Uncommon)]
        [TestCase(0.1f, ItemTier.Uncommon)]
        [TestCase(0.2f, ItemTier.Uncommon)]
        [TestCase(0.3f, ItemTier.Uncommon)]
        [TestCase(0.4f, ItemTier.Uncommon)]
        [TestCase(0.5f, ItemTier.Rare)]
        [TestCase(0.6f, ItemTier.Rare)]
        [TestCase(0.7f, ItemTier.Rare)]
        [TestCase(0.8f, ItemTier.Epic)]
        [TestCase(0.9f, ItemTier.Epic)]
        [TestCase(0.95f, ItemTier.Unstable)]
        [TestCase(0.97f, ItemTier.Unstable)]
        [TestCase(0.99f, ItemTier.Legendary)]
        [TestCase(1.0f, ItemTier.Legendary)]
        public void Choose_ReturnsExpectedTier(float alpha, ItemTier expectedTier)
        {
            for (int i = 0; i < 100; i++)
            {
                IItemConfig config = _chooser.ChooseRandomItem(alpha);

                Assert.AreEqual(expectedTier, config.Tier,
                    $"Tier dismatch, iteration: {i + 1}");
            }
        }
    }
}
