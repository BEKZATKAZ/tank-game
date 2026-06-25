using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Generation
{
    public class TilemapGenerationTests
    {
        private readonly Dictionary<int2, DirectionFlags> _directions = new()
        {
            [new int2(+1, 0)] = DirectionFlags.Right,
            [new int2(-1, 0)] = DirectionFlags.Left,

            [new int2(0, +1)] = DirectionFlags.Up,
            [new int2(0, -1)] = DirectionFlags.Down
        };

        private ITilemapGenerator _tilemapGenerator;

        [SetUp]
        public void Setup()
        {
            _tilemapGenerator = new TilemapGenerator();
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void OneCell_ReturnsTilesForEachWall(bool leftWall, bool bottomWall)
        {
            int2 origin = new int2(0, 0);

            for (int wallWidth = 1; wallWidth <= 5; wallWidth++)
            {
                MazeCell[] cells = { new MazeCell(origin) };

                cells[0].SetWallLeft(leftWall);
                cells[0].SetWallBottom(bottomWall);
                cells[0].Visit();

                using var result = _tilemapGenerator.Generate(cells, new TilemapGenerationSettings
                {
                    Seed = 4019,
                    TileSpacing = 1,
                    WallWidth = wallWidth
                });

                // (Corner + Left Wall + Bottom Wall) * Width²
                int tileCount = 1 + (leftWall ? 1 : 0) + (bottomWall ? 1 : 0);
                tileCount *= math.square(wallWidth);

                Assert.AreEqual(tileCount, result.Value.Count);
                TestBlock(result, origin, wallWidth);

                if (leftWall) TestBlock(result, origin + new int2(0, wallWidth), wallWidth);
                if (bottomWall) TestBlock(result, origin + new int2(wallWidth, 0), wallWidth);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void MultipleCells_ReturnsTilesForEachWall(int wallWidth)
        {
            MazeCell[] cells =
            {
                new MazeCell(new int2(0, 0)),
                new MazeCell(new int2(1, 0)),
                new MazeCell(new int2(2, 0))
            };

            /* │  │ *
             * └──┘ */

            cells[0].SetWallLeft(true);
            cells[0].SetWallBottom(true);
            cells[0].Visit();

            cells[1].SetWallLeft(false);
            cells[1].SetWallBottom(true);
            cells[1].Visit();

            cells[2].SetWallLeft(true);
            cells[2].SetWallBottom(false);
            cells[2].Visit();

            using var result = _tilemapGenerator.Generate(cells, new TilemapGenerationSettings
            {
                Seed = 1999,
                TileSpacing = 1,
                WallWidth = wallWidth
            });

            // 7 blocks * Width²
            int tileCount = 7 * math.square(wallWidth);
            Assert.AreEqual(tileCount, result.Value.Count);
        }

        private void TestBlock(
            TemporaryArrayResult<GeneratedTile> tiles, int2 position, int width)
        {
            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TestTile(tiles, position + new int2(x, y));
                }
            }
        }

        private void TestTile(
            TemporaryArrayResult<GeneratedTile> tiles, int2 position)
        {
            GeneratedTile origin = tiles.Value.First(x => math.all(x.Position == position));
            DirectionFlags expectedDirections = DirectionFlags.None;

            foreach ((int2 direction, DirectionFlags flags) in _directions)
            {
                int2 newPosition = position + direction;

                if (tiles.Value.Any(x => math.all(x.Position == newPosition)) == false) continue;

                expectedDirections |= flags;
            }

            Assert.AreEqual(expectedDirections, origin.Directions);
        }
    }
}
