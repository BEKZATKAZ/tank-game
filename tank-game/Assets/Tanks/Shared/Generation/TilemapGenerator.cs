using System;
using System.Runtime.CompilerServices;
using Tanks.Shared.Generation.Mazes;
using Tanks.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Generation
{
    public sealed class TilemapGenerator : ITilemapGenerator
    {
        public unsafe TemporaryArrayResult<GeneratedTile> Generate(
            ReadOnlySpan<MazeCell> cells, TilemapGenerationSettings settings, int2 resolution)
        {
            int tileCount = cells.Length * settings.WallWidth;
            using var tiles = new NativeList<int2>(tileCount, Allocator.TempJob);

            foreach (MazeCell cell in cells) GenerateBlocks(cell, tiles, settings, resolution);
            var result = new TemporaryArrayResult<GeneratedTile>(tiles.Length);

            try
            {
                // HashSet for faster neighbor look-ups in DirectionSetterJob
                using var tileSet = new NativeParallelHashSet<int2>(tiles.Length, Allocator.TempJob);
                foreach (int2 tile in tiles) tileSet.Add(tile);

                // Used to get the pointer
                ref GeneratedTile firstTile = ref result[0];

                DirectionSetterJob job = new DirectionSetterJob
                {
                    Tiles = tiles,
                    TileSet = tileSet,
                    Output = (GeneratedTile*)Unsafe.AsPointer(ref firstTile)
                };

                JobHandle handle = job.Schedule(tiles.Length, 64);
                handle.Complete();
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private static void GenerateBlocks(
            in MazeCell cell, NativeList<int2> tiles,
            TilemapGenerationSettings settings,
            int2 resolution)
        {
            int wallWidth = settings.WallWidth;
            int2 position = cell.Position * new int2(wallWidth * 2);

            // Add variation by hiding some cells
            if (cell.Position.x > 0 && cell.Position.y > 0 &&
                cell.Position.x < resolution.x && cell.Position.y < resolution.y)
            {
                float seedX = settings.Seed * 0.001f;
                float seedY = settings.Seed * 0.002f;

                float threshold = math.lerp(0.4f, 0.8f, settings.Seed % 1f);
                float noise = Mathf.PerlinNoise(
                    seedX + cell.Position.x * 10f,
                    seedY - cell.Position.y * 10f);

                // Prevent weird looking walls without a corner
                if (noise < threshold && (cell.WallLeft || cell.WallBottom)) return;
            }

            /* │ **********************************
             * └── <-- Corner (represented as └) */
            GenerateBlock(tiles, position, settings);

            if (cell.WallLeft)
            {
                /* │ <-- Top (represented as │)
                 * └── *************************/
                GenerateBlock(tiles, position + new int2(0, wallWidth), settings);
            }

            if (cell.WallBottom)
            {
                /* │ ***********************************
                 * └── <-- Bottom (represented as ──) */
                GenerateBlock(tiles, position + new int2(wallWidth, 0), settings);
            }
        }

        private static void GenerateBlock(
            NativeList<int2> destination,
            int2 origin,
            TilemapGenerationSettings settings)
        {
            for (int y = 0; y < settings.WallWidth; y++)
            {
                for (int x = 0; x < settings.WallWidth; x++)
                {
                    int2 position = origin + new int2(x, y);
                    destination.Add(position);
                }
            }
        }
    }
}
