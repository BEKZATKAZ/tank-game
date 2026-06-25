using System;
using Tanks.Shared.Generation.Mazes;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.Shared.Generation
{
    public readonly struct GeneratedTile
    {
        public GeneratedTile(int2 position, DirectionFlags directions)
        {
            Position = position;
            Directions = directions;
        }

        public readonly int2 Position;
        public readonly DirectionFlags Directions;
    }

    public struct TilemapGenerationSettings
    {
        public uint Seed;
        public int WallWidth;
        public int TileSpacing;
    }

    public interface ITilemapGenerator
    {
        TemporaryArrayResult<GeneratedTile> Generate(
            ReadOnlySpan<MazeCell> cells, TilemapGenerationSettings settings);
    }
}
