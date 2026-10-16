using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Generation.Crates;
using Tanks.Shared.Generation;
using Tanks.Shared.Items.Data;
using Unity.Mathematics;

namespace Tanks.Server.Tests.Generation
{
    public class CrateGeneratorTests
    {
        private CrateGenerator _generator;

        [SetUp]
        public void Setup()
        {
            _generator = new CrateGenerator();
        }

        [Test]
        public void Generate_EmptyTiles_ReturnsEmpty()
        {
            GeneratedTile[] emptyTiles = new GeneratedTile[0];
            var settings = CreateDefaultSettings();

            using var result = _generator.Generate(
                emptyTiles,
                seed: 12345,
                settings);

            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Generate_SameSeedAndTiles_ReturnsDeterministicResult()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 30, height: 30);
            var settings = CreateDefaultSettings();
            uint seed = 42;

            using var runA = _generator.Generate(tiles, seed, settings);
            List<GeneratedCrate> listA = ToList(runA);

            using var runB = _generator.Generate(tiles, seed, settings);
            List<GeneratedCrate> listB = ToList(runB);

            Assert.AreEqual(listA.Count, listB.Count);

            for (int i = 0; i < listA.Count; i++)
            {
                Assert.AreEqual(listA[i].Position, listB[i].Position);
                Assert.AreEqual(listA[i].Tier, listB[i].Tier);
            }
        }

        [Test]
        public void Generate_AllCratesSnapToEvenCoordinates()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 40, height: 40);
            var settings = CreateDefaultSettings();

            using var result = _generator.Generate(tiles, seed: 100, settings);

            Assert.Greater(result.Length, 0);

            for (int i = 0; i < result.Length; i++)
            {
                GeneratedCrate crate = result[i];

                Assert.AreEqual(
                    0,
                    crate.Position.x % 2,
                    $"Crate at {crate.Position} x is not snapped to even 2x2 grid");

                Assert.AreEqual(
                    0,
                    crate.Position.y % 2,
                    $"Crate at {crate.Position} y is not snapped to even 2x2 grid");
            }
        }

        [Test]
        public void Generate_CratesDoNotOverlapEachOther()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 40, height: 40);
            var settings = CreateDefaultSettings();

            using var result = _generator.Generate(tiles, seed: 777, settings);

            HashSet<int2> occupiedTiles = new HashSet<int2>();

            for (int i = 0; i < result.Length; i++)
            {
                int2 pos = result[i].Position;

                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        int2 tile = pos + new int2(dx, dy);

                        bool added = occupiedTiles.Add(tile);

                        Assert.IsTrue(
                            added,
                            $"Crate at {pos} overlaps another crate at tile {tile}");
                    }
                }
            }
        }

        [Test]
        public void Generate_CratesDoNotOverlapWalls()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 40, height: 40);
            HashSet<int2> wallTiles = new HashSet<int2>();

            for (int i = 0; i < tiles.Length; i++)
            {
                wallTiles.Add(tiles[i].Position);
            }

            var settings = CreateDefaultSettings();

            using var result = _generator.Generate(tiles, seed: 999, settings);

            for (int i = 0; i < result.Length; i++)
            {
                int2 pos = result[i].Position;

                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        int2 tile = pos + new int2(dx, dy);

                        Assert.IsFalse(
                            wallTiles.Contains(tile),
                            $"Crate at {pos} placed on wall tile {tile}");
                    }
                }
            }
        }

        [Test]
        public void Generate_ZeroEpicCount_ProducesNoEpicCrates()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 40, height: 40);
            var settings = CreateDefaultSettings();
            settings.EpicCrateCount = 0;

            using var result = _generator.Generate(tiles, seed: 123, settings);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreNotEqual(
                    ItemTier.Epic,
                    result[i].Tier,
                    "Expected no Epic crates when EpicCrateCount is 0");
            }
        }

        [Test]
        public void Generate_ZeroRareDensity_ProducesNoRareCrates()
        {
            GeneratedTile[] tiles = CreateBoxMap(width: 40, height: 40);
            var settings = CreateDefaultSettings();
            settings.RareCrateDensity = 0f;

            using var result = _generator.Generate(tiles, seed: 123, settings);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreNotEqual(
                    ItemTier.Rare,
                    result[i].Tier,
                    "Expected no Rare crates when RareCrateDensity is 0");
            }
        }

        private static CrateGenerationSettings CreateDefaultSettings()
        {
            return new CrateGenerationSettings
            {
                ClusterDensity = 8,
                ClusterRadius = 8,
                MinClusterDistance = 15,
                MinMaxCratesPerCluster = new int2(4, 10),
                CornerBias = 4,

                EpicCrateCount = 2,
                EpicRadius = 4,

                RareCrateDensity = 5,
                MinSingleCrateDistance = 10
            };
        }

        private static GeneratedTile[] CreateBoxMap(int width, int height)
        {
            List<GeneratedTile> tiles = new List<GeneratedTile>();

            for (int x = 0; x < width; x++)
            {
                tiles.Add(new GeneratedTile(new int2(x, 0), DirectionFlags.None));
                tiles.Add(new GeneratedTile(new int2(x, height - 1), DirectionFlags.None));
            }

            for (int y = 1; y < height - 1; y++)
            {
                tiles.Add(new GeneratedTile(new int2(0, y), DirectionFlags.None));
                tiles.Add(new GeneratedTile(new int2(width - 1, y), DirectionFlags.None));
            }

            return tiles.ToArray();
        }

        private static List<GeneratedCrate> ToList(
            Tanks.Utils.TemporaryArrayResult<GeneratedCrate> array)
        {
            List<GeneratedCrate> list = new List<GeneratedCrate>(array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                list.Add(array[i]);
            }

            return list;
        }
    }
}
