using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Tanks.Shared.Generation
{
    [BurstCompile]
    public unsafe struct DirectionSetterJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeList<int2> Tiles;

        [ReadOnly]
        public NativeParallelHashSet<int2> TileSet;

        [NativeDisableUnsafePtrRestriction]
        public GeneratedTile* Output;

        public void Execute(int index)
        {
            int2 origin = Tiles[index];
            DirectionFlags directionResult = DirectionFlags.None;

            if (TileSet.Contains(origin + new int2(1, 0)))
            {
                directionResult |= DirectionFlags.Right;
            }

            if (TileSet.Contains(origin + new int2(0, 1)))
            {
                directionResult |= DirectionFlags.Up;
            }

            if (TileSet.Contains(origin + new int2(-1, 0)))
            {
                directionResult |= DirectionFlags.Left;
            }

            if (TileSet.Contains(origin + new int2(0, -1)))
            {
                directionResult |= DirectionFlags.Down;
            }

            Output[index] = new GeneratedTile(origin, directionResult);
        }
    }
}