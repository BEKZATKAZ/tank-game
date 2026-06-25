using NUnit.Framework;
using System.Linq;
using Tanks.Shared.Generation.Mazes;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Generation
{
    public class MazeGenerationTests
    {
        private IMazeGenerator _generator;

        [SetUp]
        public void Setup()
        {
            _generator = new DFSGenerator();
        }

        [TestCase(4, 2)]
        [TestCase(8, 4)]
        [TestCase(16, 8)]
        [TestCase(32, 16)]
        public void SameSeeds_ReturnsSameResults(int sizeX, int sizeY)
        {
            const uint seed = 45001;

            int2 resolution = new int2(sizeX, sizeY);
            using var expected = _generator.Generate(seed, resolution);

            Assert.AreEqual((sizeX + 1) * (sizeY + 1), expected.Value.Count,
                "Resolution and result do not match");

            for (int x = 0; x <= resolution.x; x++)
            {
                int2 position = new int2(x, resolution.y);
                MazeCell edge = expected.Value.FirstOrDefault(x => math.all(x.Position == position));

                Assert.AreNotEqual(default, edge.Flags, $"Failed to find cell at {position}");
                Assert.True(edge.WallBottom, $"Unsealed top at {position}");
            }

            for (int y = 0; y <= resolution.y; y++)
            {
                int2 position = new int2(resolution.x, y);
                MazeCell edge = expected.Value.FirstOrDefault(x => math.all(x.Position == position));

                Assert.AreNotEqual(default, edge.Flags, $"Failed to find cell at {position}");
                Assert.True(edge.WallLeft, $"Unsealed right at {position}");
            }

            for (int i = 0; i < 3; i++)
            {
                using var actual = _generator.Generate(seed, resolution);
                Assert.That(expected.Value, Is.EqualTo(actual.Value));
            }
        }

        [TestCase(4, 4)]
        [TestCase(8, 8)]
        [TestCase(16, 16)]
        public void DifferentSeeds_ReturnsDifferentResults(int sizeX, int sizeY)
        {
            const uint seed = 12906;

            int2 resolution = new int2(sizeX, sizeY);
            using var expected = _generator.Generate(seed, resolution);

            for (uint i = 1; i <= 10; i++)
            {
                using var actual = _generator.Generate(seed + i, resolution);
                Assert.That(expected.Value, Is.Not.EqualTo(actual.Value));
            }
        }
    }
}
