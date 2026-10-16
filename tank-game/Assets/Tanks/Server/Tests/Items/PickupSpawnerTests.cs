using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Items.Pickups;
using Tanks.Shared;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Unity.Mathematics;

namespace Tanks.Server.Tests.Items
{
    public class PickupSpawnerTests
    {
        // Longer than MaxCooldownSeconds, so one call is always exactly one spawn attempt
        private const float PastAnyCooldown = 25f;

        private PickupSpawner _component;
        private ISessionSettingsService _settings;
        private IPickupSpawnBehaviour _behaviour;
        private IItemConfig[] _itemCollection;
        private List<PickupSpawned> _spawned;
        private int _tick;

        [SetUp]
        public void Setup()
        {
            IItemDatabase items = Substitute.For<IItemDatabase>();
            IMessageBus bus = Substitute.For<IMessageBus>();

            _itemCollection = GetItems().ToArray();
            items.Items.Returns(_itemCollection);

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().ItemSpawn.Returns(new ItemSpawnSettings
            {
                MinCooldownSeconds = 10,
                MaxCooldownSeconds = 20,
                MinDistanceToPlayers = 2
            });

            _behaviour = Substitute.For<IPickupSpawnBehaviour>();
            _behaviour.ItemCounts.Returns(new Dictionary<ItemTier, int>()
            {
                [ItemTier.Uncommon] = 35,
                [ItemTier.Rare] = 24,
                [ItemTier.Epic] = 10,
                [ItemTier.Unstable] = 5,
                [ItemTier.Legendary] = 1
            });

            // Recording the calls keeps the assertions in the tests instead of in a callback,
            // where a failure points at the spawner call and not at the broken expectation
            _spawned = new List<PickupSpawned>();
            _behaviour
                .When(x => x.SpawnEveryoneRpc(Arg.Any<PickupSpawned>()))
                .Do(call => _spawned.Add(call.Arg<PickupSpawned>()));

            _component = new PickupSpawner(items, bus, _settings);
            _component.Initialize(_behaviour);
        }

        [Test]
        public void Spawn_DoesNothing_BeforeGameStarted()
        {
            GenerateTilemap(new int2(10, 5));

            Advance(PastAnyCooldown);

            Assert.AreEqual(0, _spawned.Count);
        }

        [Test]
        public void Spawn_DoesNothing_BeforeTilemapGenerated()
        {
            StartGame();

            Assert.DoesNotThrow(() => Advance(PastAnyCooldown));
            Assert.AreEqual(0, _spawned.Count);
        }

        [Test]
        public void Spawn_HappensImmediately_AfterTilemapGenerated()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));

            Assert.AreEqual(0, _component.CurrentCooldown,
                "A new tilemap should reset the cooldown");

            Advance(0.1f);

            Assert.AreEqual(1, _spawned.Count);
        }

        [Test]
        [Repeat(10)]
        public void Cooldown_IsWithinConfiguredRange_AfterSpawning()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));

            Advance(0.1f);

            ItemSpawnSettings settings = _settings.GetSettings().ItemSpawn;
            float cooldown = _component.CurrentCooldown;

            Assert.GreaterOrEqual(
                cooldown, settings.MinCooldownSeconds, "Cooldown is below min");
            Assert.LessOrEqual(
                cooldown, settings.MaxCooldownSeconds, "Cooldown is above max");
        }

        [Test]
        public void Cooldown_BlocksSpawning_UntilElapsed()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));
            Advance(0.1f);

            float cooldown = _component.CurrentCooldown;
            _spawned.Clear();

            Advance(cooldown - 0.5f);
            Assert.AreEqual(0, _spawned.Count, "Spawned before the cooldown elapsed");

            Advance(1f);
            Assert.AreEqual(1, _spawned.Count, "Did not spawn after the cooldown elapsed");
        }

        [Test]
        public void Cooldown_Restarts_AfterSpawning()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));
            Advance(PastAnyCooldown);
            Advance(PastAnyCooldown);
            _spawned.Clear();

            float minCooldown = _settings.GetSettings().ItemSpawn.MinCooldownSeconds;
            Advance(minCooldown - 0.5f);

            Assert.AreEqual(0, _spawned.Count,
                "The previous spawn did not reset the elapsed time");
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public void Spawn_WithinBounds(int wallWidth)
        {
            int2 resolution = new int2(10, 5);

            StartGame();
            GenerateTilemap(resolution, wallWidth);
            SpawnPickups(30);

            Assert.Greater(_spawned.Count, 0,
                "Nothing spawned, the bounds were never checked");

            // The maze has `resolution` cells per axis. The extra row and column that the
            // generator adds only seal the maze, so a pickup there would sit inside a wall
            float2 min = wallWidth * 1.5f;
            float2 max = (float2)(resolution - 1) * (wallWidth * 2) + min;

            foreach (PickupSpawned pickup in _spawned)
            {
                bool isOutside = math.any(pickup.WorldPosition < min) ||
                                 math.any(pickup.WorldPosition > max);

                Assert.IsFalse(isOutside,
                    $"Pickup is out of bounds: '{pickup.WorldPosition}'");
            }
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public void Spawn_AtCellCenters(int wallWidth)
        {
            StartGame();
            GenerateTilemap(new int2(10, 5), wallWidth);
            SpawnPickups(10);

            Assert.Greater(_spawned.Count, 0);

            foreach (PickupSpawned pickup in _spawned)
            {
                int2 cell = ToCell(pickup.WorldPosition, wallWidth);
                float2 expected = ToWorld(cell, wallWidth);

                Assert.IsTrue(
                    math.all(math.abs(pickup.WorldPosition - expected) < 0.0001f),
                    $"'{pickup.WorldPosition}' is not the center of a cell");
            }
        }

        [Test]
        [Repeat(10)]
        public void Spawn_NeverStacksPickups()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));

            SpawnPickups(30);

            Assert.AreEqual(30, _spawned.Count,
                "The maze has room for more pickups than this");
            AssertNoDuplicatePositions();
        }

        [Test]
        [Repeat(10)]
        public void Spawn_UsesKnownItems()
        {
            StartGame();
            GenerateTilemap(new int2(10, 5));
            SpawnPickups(20);

            Assert.Greater(_spawned.Count, 0);

            foreach (PickupSpawned pickup in _spawned)
            {
                bool isKnown = _itemCollection.Any(x => x.Identifier == pickup.ItemId);
                Assert.IsTrue(isKnown,
                    $"Where did this item ID come from?: '{pickup.ItemId}'");
            }
        }

        [Test]
        public void Spawn_Stops_WhenEveryCellIsOccupied()
        {
            StartGame();
            GenerateTilemap(new int2(3, 3));

            // Far more attempts than the tiny maze has cells
            SpawnPickups(100);

            int filled = _spawned.Count;
            Assert.Greater(filled, 0);
            AssertNoDuplicatePositions();

            Advance(PastAnyCooldown);
            Advance(PastAnyCooldown);

            Assert.AreEqual(filled, _spawned.Count, "Spawned into a full maze");
        }

        [Test]
        public void Spawn_ReusesCell_AfterItIsReleased()
        {
            StartGame();
            GenerateTilemap(new int2(3, 3));
            SpawnPickups(100);

            float2 released = _spawned[0].WorldPosition;
            int filled = _spawned.Count;

            _component.ReleaseCell(released);

            // Retries are allowed, finding the single free cell is random
            for (int i = 0; i < 10 && _spawned.Count == filled; i++)
            {
                Advance(PastAnyCooldown);
            }

            Assert.AreEqual(filled + 1, _spawned.Count, "Nothing spawned into the free cell");
            Assert.AreEqual(released, _spawned.Last().WorldPosition);
        }

        [Test]
        public void Spawn_ReusesCells_AfterNewTilemap()
        {
            StartGame();
            GenerateTilemap(new int2(3, 3));
            SpawnPickups(100);

            int filled = _spawned.Count;

            GenerateTilemap(new int2(3, 3));
            Advance(0.1f);

            Assert.AreEqual(filled + 1, _spawned.Count,
                "Cells of the previous maze still block the new one");
        }

        [Test]
        public void ReleaseCell_DoesNothing_BeforeTilemapGenerated()
        {
            Assert.DoesNotThrow(() => _component.ReleaseCell(new float2(3, 3)));
        }

        private static int2 ToCell(float2 position, int wallWidth)
        {
            return (int2)math.round((position - wallWidth * 1.5f) / (wallWidth * 2));
        }

        private static float2 ToWorld(int2 cell, int wallWidth)
        {
            return (float2)cell * (wallWidth * 2) + wallWidth * 1.5f;
        }

        private static IEnumerable<IItemConfig> GetItems()
        {
            foreach (ItemTier tier in Enum.GetValues(typeof(ItemTier)))
            {
                string tierName = tier.ToString();

                for (int i = 1; i <= 4; i++)
                {
                    yield return new MockItemConfig
                    {
                        // For example "uncommon-4"
                        Identifier = new ItemIdentifier($"{tierName}-{i}"),
                        Tier = tier
                    };
                }
            }
        }

        private void StartGame()
        {
            _component.Handle(new GameStartedFromServer { StartDelaySeconds = 0 }, null);
        }

        private void GenerateTilemap(int2 resolution, int wallWidth = 2)
        {
            _component.Handle(new TilemapGenerated
            {
                Resolution = resolution,
                Settings = new TilemapGenerationSettings
                {
                    Seed = 1,
                    TileSpacing = 1,
                    WallWidth = wallWidth
                }
            }, null);
        }

        private void Advance(float seconds)
        {
            _component.Tick(++_tick, seconds);
        }

        private void SpawnPickups(int count)
        {
            // Spawning can fail now and then when the maze is crowded,
            // so there are spare attempts
            for (int i = 0; i < count * 5 && _spawned.Count < count; i++)
            {
                Advance(PastAnyCooldown);
            }
        }

        private void AssertNoDuplicatePositions()
        {
            HashSet<float2> positions = new HashSet<float2>();

            foreach (PickupSpawned pickup in _spawned)
            {
                bool isNew = positions.Add(pickup.WorldPosition);

                Assert.IsTrue(isNew,
                    $"2 pickups at the same position '{pickup.WorldPosition}'. " +
                    $"Total pickups added: {_spawned.Count}");
            }
        }
    }
}
