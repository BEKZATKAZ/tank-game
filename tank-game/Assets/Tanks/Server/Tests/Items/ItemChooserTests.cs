using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Dictionary<ItemTier, int> _itemCounts = new()
        {
            [ItemTier.Uncommon] = 35,
            [ItemTier.Rare] = 24,
            [ItemTier.Epic] = 10,
            [ItemTier.Unstable] = 5,
            [ItemTier.Legendary] = 1
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
            _chooser = new ItemChooser(_items, _itemCounts, seed: 123);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(20)]
        public void ChooseNext_ReturnsExpectedTimes(int loops)
        {
            int totalCount = _itemCounts.Sum(x => x.Value);

            for (int i = 0; i < loops; i++)
            {
                Dictionary<ItemTier, int> result = Enumerable.Range(0, totalCount)
                    .Select(_ => _chooser.ChooseNext())
                    .GroupBy(x => x.Tier)
                    .ToDictionary(k => k.Key, v => v.Count());

                Assert.That(result.Count, Is.EqualTo(_itemCounts.Count), "Some items were lost");

                foreach ((ItemTier tier, int count) in result)
                {
                    Assert.That(_itemCounts[tier], Is.EqualTo(count));
                }
            }
        }
    }
}
