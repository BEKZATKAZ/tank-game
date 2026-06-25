using System.Collections.Generic;
using Tanks.Utils;
using Unity.Collections;
using Unity.Mathematics;

namespace Tanks.Shared.Generation.Mazes
{
    public sealed class DFSGenerator : IMazeGenerator
    {
        private readonly Stack<int> _stack = new Stack<int>();

        public TemporaryArrayResult<MazeCell> Generate(uint seed, int2 resolution)
        {
            var result = new TemporaryArrayResult<MazeCell>((resolution.x + 1) * (resolution.y + 1));

            int GetIndex(int x, int y) => y * (resolution.x + 1) + x;
            ref MazeCell GetRef(int x, int y) => ref result[GetIndex(x, y)];

            try
            {
                int currentIndex = 0;
                result[0].Visit();
                _stack.Clear();

                // Using a fixed list to avoid heap allocations
                // 64 bytes are enough for 4 neighbor indices
                FixedList64Bytes<int> unvisitedNeighbors = new();
                Random random = Random.CreateFromIndex(seed);
                int index = 0;

                for (int y = 0; y <= resolution.y; y++)
                {
                    for (int x = 0; x <= resolution.x; x++)
                    {
                        int2 position = new int2(x, y);
                        GetRef(x, y) = new MazeCell(position);
                        index++;
                    }
                }

                while (true)
                {
                    ref MazeCell current = ref result[currentIndex];
                    (int x, int y) = (current.Position.x, current.Position.y);
                    unvisitedNeighbors.Clear();

                    void TryAddUnvisited(int x, int y)
                    {
                        if (x < 0 || x >= resolution.x) return;
                        if (y < 0 || y >= resolution.y) return;
                        if (GetRef(x, y).IsVisited) return;

                        unvisitedNeighbors.Add(GetIndex(x, y));
                    }

                    TryAddUnvisited(x - 1, y);
                    TryAddUnvisited(x + 1, y);
                    TryAddUnvisited(x, y - 1);
                    TryAddUnvisited(x, y + 1);

                    if (unvisitedNeighbors.Length == 0)
                    {
                        if (_stack.Count == 0) break;
                        currentIndex = _stack.Pop();
                        continue;
                    }

                    int unvisitedIndex = random.NextInt(0, unvisitedNeighbors.Length);
                    int chosenIndex = unvisitedNeighbors[unvisitedIndex];

                    ref MazeCell chosen = ref result[chosenIndex];
                    RemoveWall(ref result[currentIndex], ref chosen);

                    _stack.Push(currentIndex);
                    chosen.Visit();
                    currentIndex = chosenIndex;
                }

                // Seal very right
                for (int y = 0; y < resolution.y; y++)
                {
                    ref MazeCell cell = ref GetRef(resolution.x, y);
                    cell.SetWallLeft(true);
                    cell.SetWallBottom(false);
                }

                // Seal very top
                for (int x = 0; x < resolution.x; x++)
                {
                    ref MazeCell cell = ref GetRef(x, resolution.y);
                    cell.SetWallLeft(false);
                    cell.SetWallBottom(true);
                }

                ref MazeCell last = ref GetRef(resolution.x, resolution.y);
                last.SetWallLeft(true);
                last.SetWallBottom(true);

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private static void RemoveWall(ref MazeCell a, ref MazeCell b)
        {
            if (a.Position.x == b.Position.x)
            {
                if (a.Position.y > b.Position.y) a.SetWallBottom(false);
                else b.SetWallBottom(false);
                return;
            }

            if (a.Position.x > b.Position.x) a.SetWallLeft(false);
            else b.SetWallLeft(false);
        }
    }
}
