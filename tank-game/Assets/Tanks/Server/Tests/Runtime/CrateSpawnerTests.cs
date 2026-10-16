using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Editor.TestHelpers;
using Tanks.Server.Entities;
using Tanks.Server.Generation.Crates;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Utils;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Tanks.Server.Tests.Runtime
{
    public class CrateSpawnerTests
    {
        private class FakeCrateGenerator : ICrateGenerator
        {
            public int GenerateCallCount { get; private set; }
            public uint LastSeed { get; private set; }
            public int LastTileCount { get; private set; }
            public TemporaryArrayResult<GeneratedCrate> ResultToReturn { get; set; }

            public TemporaryArrayResult<GeneratedCrate> Generate(
                ReadOnlySpan<GeneratedTile> tiles,
                uint seed,
                CrateGenerationSettings settings)
            {
                GenerateCallCount++;
                LastSeed = seed;
                LastTileCount = tiles.Length;

                return ResultToReturn;
            }
        }

        private GameObject _spawnerGo;
        private CrateSpawner _spawner;
        private FakeCrateGenerator _generator;
        private IObjectResolver _container;

        private CrateBehaviour _uncommonCrate;
        private CrateBehaviour _rareCrate;
        private CrateBehaviour _epicCrate;

        private ObjectPool<ICrateBehaviour> _uncommonPool;
        private ObjectPool<ICrateBehaviour> _rarePool;
        private ObjectPool<ICrateBehaviour> _epicPool;

        [SetUp]
        public void Setup()
        {
            _generator = new FakeCrateGenerator();
            _container = Substitute.For<IObjectResolver>();

            _spawnerGo = new GameObject("CrateSpawner");
            _spawner = _spawnerGo.AddComponent<CrateSpawner>();

            _spawner.Construct(_generator, _container);

            _uncommonCrate = CreateObject();
            _rareCrate = CreateObject();
            _epicCrate = CreateObject();

            _uncommonPool = new ObjectPool<ICrateBehaviour>(createFunc: () => _uncommonCrate);
            _rarePool = new ObjectPool<ICrateBehaviour>(createFunc: () => _rareCrate);
            _epicPool = new ObjectPool<ICrateBehaviour>(createFunc: () => _epicCrate);

            var pools = _spawner.GetPrivateField<
                Dictionary<ItemTier, ObjectPool<ICrateBehaviour>>>("_pools");

            pools[ItemTier.Uncommon] = _uncommonPool;
            pools[ItemTier.Rare] = _rarePool;
            pools[ItemTier.Epic] = _epicPool;
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawnerGo)
            {
                UnityEngine.Object.DestroyImmediate(_spawnerGo);
            }

            if (_uncommonCrate)
            {
                UnityEngine.Object.DestroyImmediate(_uncommonCrate.gameObject);
            }

            if (_rareCrate)
            {
                UnityEngine.Object.DestroyImmediate(_rareCrate.gameObject);
            }

            if (_epicCrate)
            {
                UnityEngine.Object.DestroyImmediate(_epicCrate.gameObject);
            }
        }

        [Test]
        public void Spawn_CallsGeneratorWithTilesAndSeed()
        {
            GeneratedTile[] tiles = new[]
            {
                new GeneratedTile(new int2(0, 0), DirectionFlags.None)
            };

            uint seed = 98765;

            _generator.ResultToReturn = new TemporaryArrayResult<GeneratedCrate>(0);
            _spawner.Spawn(tiles, seed);

            Assert.AreEqual(1, _generator.GenerateCallCount);
            Assert.AreEqual(seed, _generator.LastSeed);
            Assert.AreEqual(1, _generator.LastTileCount);
        }

        [Test]
        public void Spawn_ActivatesInstancesAndOffsetsPosition()
        {
            GeneratedTile[] tiles = new[]
            {
                new GeneratedTile(new int2(0, 0), DirectionFlags.None)
            };

            using var generated = new TemporaryArrayResult<GeneratedCrate>(1);
            generated[0] = new GeneratedCrate(new int2(10, 20), ItemTier.Uncommon);

            _generator.ResultToReturn = generated;
            _spawner.Spawn(tiles, seed: 1);

            Assert.AreEqual(new Vector3(11f, 21f, 0f), _uncommonCrate.transform.position);
        }

        [Test]
        public void Spawn_TracksActiveCrates()
        {
            GeneratedTile[] tiles = new[]
            {
                new GeneratedTile(new int2(0, 0), DirectionFlags.None)
            };

            using var generated = new TemporaryArrayResult<GeneratedCrate>(2);

            generated[0] = new GeneratedCrate(new int2(2, 4), ItemTier.Uncommon);
            generated[1] = new GeneratedCrate(new int2(6, 8), ItemTier.Rare);

            _generator.ResultToReturn = generated;
            _spawner.Spawn(tiles, seed: 1);

            var activeCrates = _spawner.GetPrivateField<
                Dictionary<ItemTier, List<ICrateBehaviour>>>("_activeCrates");

            Assert.AreEqual(2, activeCrates.Count);
            Assert.AreEqual(_uncommonCrate, activeCrates[ItemTier.Uncommon][0]);
            Assert.AreEqual(_rareCrate, activeCrates[ItemTier.Rare][0]);
        }

        [Test]
        public void Clear_ReturnsActiveCratesToPoolAndDeactivates()
        {
            GeneratedTile[] tiles = new[]
            {
                new GeneratedTile(new int2(0, 0), DirectionFlags.None)
            };

            using var generated = new TemporaryArrayResult<GeneratedCrate>(2);

            generated[0] = new GeneratedCrate(new int2(2, 4), ItemTier.Uncommon);
            generated[1] = new GeneratedCrate(new int2(6, 8), ItemTier.Epic);

            _generator.ResultToReturn = generated;
            _spawner.Spawn(tiles, seed: 1);
            _spawner.Clear();

            var activeCrates = _spawner.GetPrivateField<
                Dictionary<ItemTier, List<ICrateBehaviour>>>("_activeCrates");

            Assert.AreEqual(0, activeCrates.Sum(x => x.Value.Count),
                "All active crate lists should be empty after Clear()");
        }

        [Test]
        public void Clear_WhenNoActiveCrates_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _spawner.Clear());
        }

        private static CrateBehaviour CreateObject()
        {
            return new GameObject("Crate", typeof(HealthBehaviour))
                .AddComponent<CrateBehaviour>();
        }
    }
}
