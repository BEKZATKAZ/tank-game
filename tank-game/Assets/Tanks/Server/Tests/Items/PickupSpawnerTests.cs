using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private PickupSpawner _component;
        private IItemConfig[] _itemCollection;

        [SetUp]
        public void Setup()
        {
            IItemDatabase items = Substitute.For<IItemDatabase>();
            IMessageBus bus = Substitute.For<IMessageBus>();

            _component = new PickupSpawner(items, bus);
            _component.Initialize(Substitute.For<IPickupSpawnBehaviour>());

            _component.Behaviour.MinCooldown.Returns(10);
            _component.Behaviour.MaxCooldown.Returns(20);
            _itemCollection = GetItems().ToArray();

            items.IsReady.Returns(true);
            items.Items.Returns(_itemCollection);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public void Spawn_WithinBounds(int wallWidth)
        {
            int tick = 1009;
            int2 resolution = new int2(10, 5);
            HashSet<float2> previousPositions = new HashSet<float2>();

            float2 min = wallWidth * 1.5f;
            float2 max = (float2)resolution * (wallWidth * 2) + min;

            _component.Behaviour.SpawnEveryoneRpc(
                Arg.Do<PickupSpawned>(x => ValidateSpawn(x, min, max, previousPositions)));

            _component.Handle(new GameStarted { StartDelaySeconds = 0 }, null);

            for (int i = 0; i < 15; i++)
            {
                _component.Handle(new TilemapGenerated
                {
                    Resolution = resolution,
                    Settings = new TilemapGenerationSettings
                    {
                        Seed = (uint)i,
                        TileSpacing = 1,
                        WallWidth = wallWidth
                    }
                }, null);

                Assert.AreEqual(0, _component.CurrentCooldown,
                    "Cooldown should be set after Tick()");

                _component.Tick(++tick, 0.1f);

                float cooldown = _component.CurrentCooldown;
                float minCooldown = _component.Behaviour.MinCooldown;
                float maxCooldown = _component.Behaviour.MaxCooldown;

                Assert.GreaterOrEqual(cooldown, minCooldown, "Cooldown is below min");
                Assert.LessOrEqual(cooldown, maxCooldown, "Cooldown is above max");

                _component.Behaviour.ClearReceivedCalls();

                for (float elapsed = 0.1f; elapsed < cooldown - 0.2f; elapsed += 0.1f)
                {
                    _component.Tick(++tick, 0.1f);

                    Assert.AreEqual(cooldown, _component.CurrentCooldown,
                        "Cooldown changed before spawning");
                }

                _component.Behaviour.DidNotReceive().SpawnEveryoneRpc(Arg.Any<PickupSpawned>());
                _component.Tick(++tick, 0.5f);
                _component.Behaviour.Received(1).SpawnEveryoneRpc(Arg.Any<PickupSpawned>());

                Assert.AreNotEqual(cooldown, _component.CurrentCooldown,
                    "Cooldown should change after spawning");
            }
        }

        private void ValidateSpawn(
            PickupSpawned payload, float2 min, float2 max, HashSet<float2> previousPositions)
        {
            if (previousPositions.Add(payload.WorldPosition) == false)
            {
                Assert.Fail(
                    $"2 pickups at the same position '{payload.WorldPosition}'. " +
                    $"Total pickups added: {previousPositions.Count}");
            }

            if (math.any(payload.WorldPosition < min) ||
                math.any(payload.WorldPosition > max))
            {
                Assert.Fail($"Pickup really wanted to go out of bounds: '{payload.WorldPosition}'");
            }

            if (_itemCollection.Any(x => x.Identifier == payload.ItemId) == false)
            {
                Assert.Fail($"Where did this item ID come from?: '{payload.ItemId}'");
            }
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
    }
}
