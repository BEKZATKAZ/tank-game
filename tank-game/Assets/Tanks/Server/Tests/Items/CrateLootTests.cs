using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Generation;
using Tanks.Server.Items.Pickups;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;

namespace Tanks.Server.Tests.Items
{
    public class CrateLootTests
    {
        private sealed class FixedRandom : Random
        {
            private readonly double _nextDoubleValue;
            private readonly int _nextIntValue;

            public FixedRandom(double nextDoubleValue, int nextIntValue = 0)
            {
                _nextDoubleValue = nextDoubleValue;
                _nextIntValue = nextIntValue;
            }

            public override double NextDouble() => _nextDoubleValue;

            public override int Next(int maxValue) => Math.Min(_nextIntValue, maxValue - 1);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static IItemConfig MakeItem(ItemTier tier, string id)
        {
            IItemConfig item = Substitute.For<IItemConfig>();
            item.Tier.Returns(tier);
            item.Identifier.Returns(new ItemIdentifier(id));
            return item;
        }

        private static ICrateBehaviour MakeBehaviour(
            IReadOnlyDictionary<ItemTier, float> chances)
        {
            ICrateBehaviour behaviour = Substitute.For<ICrateBehaviour>();
            behaviour.Chances.Returns(chances);
            behaviour.CanDropItems.Returns(true);

            IHealthBehaviour health = Substitute.For<IHealthBehaviour>();
            behaviour.GetBehaviour<IHealthBehaviour>().Returns(health);

            return behaviour;
        }

        private static CrateLoot CreateLoot(
            ICrateBehaviour behaviour,
            IItemConfig[] items,
            Random random,
            out IPickupSpawnBehaviour spawner)
        {
            IItemDatabase db = Substitute.For<IItemDatabase>();
            db.Items.Returns(items);

            spawner = Substitute.For<IPickupSpawnBehaviour>();

            CrateLoot loot = new CrateLoot(db, spawner, Substitute.For<ICrateSpawner>())
            {
                Random = random
            };

            loot.Initialize(behaviour);

            return loot;
        }

        private static void AssertSpawnedTier(
            IPickupSpawnBehaviour spawner,
            IItemConfig[] items,
            ItemTier expectedTier,
            int times = 1)
        {
            IEnumerable<ItemIdentifier> tierIds = items
                .Where(x => x.Tier == expectedTier)
                .Select(x => x.Identifier);

            spawner.Received(times).SpawnEveryoneRpc(
                Arg.Is<PickupSpawned>(p => tierIds.Contains(p.ItemId)));
        }

        private static void AssertNothingSpawned(IPickupSpawnBehaviour spawner)
        {
            spawner.DidNotReceive().SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
        }

        [Test]
        public void Handle_HundredPercentChance_SpawnsSingleItem()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Uncommon] = 1
            });

            IItemConfig[] items =
            {
                MakeItem(ItemTier.Uncommon, "uncommon-a"),
                MakeItem(ItemTier.Uncommon, "uncommon-b")
            };

            // NextDouble = 0.0 → chance 1.0 > 0.0 → passes
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());

            AssertSpawnedTier(spawner, items, ItemTier.Uncommon, times: 1);
            spawner.Received(1).SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
        }

        [Test]
        public void Handle_ZeroPercentChance_NeverSpawnsItem()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Rare] = 0
            });

            IItemConfig[] items = { MakeItem(ItemTier.Rare, "rare-a") };

            // NextDouble = 0.0 → 0.0 is NOT > 0.0 → always fails
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.0),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());
            AssertNothingSpawned(spawner);
        }

        [Test]
        public void Handle_CannotDropItems_DoesNotSpawnItems()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Rare] = 1
            });

            behaviour.CanDropItems.Returns(false);
            IItemConfig[] items = { MakeItem(ItemTier.Rare, "rare-a") };

            // NextDouble = 0.0 → 0.0 is NOT > 0.0 → always fails
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.0),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());
            AssertNothingSpawned(spawner);
        }

        [Test]
        public void Handle_ChanceBelowRoll_DoesNotSpawn()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Epic] = 0.3f
            });

            IItemConfig[] items = { MakeItem(ItemTier.Epic, "epic-a") };

            // NextDouble = 0.5 → 0.3 is NOT > 0.5 → fails
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.5),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());
            AssertNothingSpawned(spawner);
        }

        [Test]
        public void Handle_ChanceAboveRoll_SpawnsSingleItem()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Legendary] = 0.8f
            });

            IItemConfig[] items = { MakeItem(ItemTier.Legendary, "legendary-a") };

            // NextDouble = 0.5 → 0.8 > 0.5 → passes
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.5),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());

            AssertSpawnedTier(spawner, items, ItemTier.Legendary, times: 1);
            spawner.Received(1).SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
        }

        [Test]
        public void Handle_MultipleTiers_OnlyOneTierHit_SpawnsThatTier()
        {
            // Uncommon = 1.0 (certain), Epic = 0.3 (misses at roll 0.5)
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Uncommon] = 1.0f,
                [ItemTier.Epic] = 0.3f
            });

            IItemConfig[] items =
            {
                MakeItem(ItemTier.Uncommon, "uncommon-a"),
                MakeItem(ItemTier.Epic, "epic-a")
            };

            // NextDouble = 0.5 → uncommon passes (1.0 > 0.5), epic fails (0.3 <= 0.5)
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.5),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());

            AssertSpawnedTier(spawner, items, ItemTier.Uncommon, times: 1);
            spawner.Received(1).SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
        }

        [Test]
        public void Handle_EmptyChancesDictionary_SpawnsNothing()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>());

            CrateLoot loot = CreateLoot(
                behaviour,
                items: Array.Empty<IItemConfig>(),
                new FixedRandom(nextDoubleValue: 0.0),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());
            AssertNothingSpawned(spawner);
        }

        [Test]
        public void Handle_MultipleTiersAllHundredPercent_DropsExactlyOneItemTops()
        {
            ICrateBehaviour behaviour = MakeBehaviour(new Dictionary<ItemTier, float>
            {
                [ItemTier.Uncommon] = 1,
                [ItemTier.Rare] = 1,
                [ItemTier.Epic] = 1
            });

            IItemConfig[] items =
            {
                MakeItem(ItemTier.Uncommon, "uncommon-a"),
                MakeItem(ItemTier.Rare, "rare-a"),
                MakeItem(ItemTier.Epic, "epic-a")
            };

            // With Next(maxValue) returning index 1, it picks the 2nd successful tier (Rare)
            CrateLoot loot = CreateLoot(
                behaviour,
                items,
                new FixedRandom(nextDoubleValue: 0.0, nextIntValue: 1),
                out IPickupSpawnBehaviour spawner);

            loot.Handle(new Died());

            AssertSpawnedTier(spawner, items, ItemTier.Rare, times: 1);
            spawner.Received(1).SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
        }
    }
}
