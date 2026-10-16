using System;
using Tanks.Shared.Generation;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Tanks.Server.Generation.Crates
{
    public sealed class CrateOccupancy : IDisposable
    {
        public const int CrateSize = 2;

        private const byte Free = 0;
        private const byte Wall = 1;
        private const byte Crate = 2;

        private readonly int2 _min;
        private readonly int _width;
        private readonly int _height;

        private NativeArray<byte> _cells;
        private NativeArray<byte> _checkpoint;

        public int OpenArea { get; }

        public int2 Center => _min + new int2(_width / 2, _height / 2);

        public CrateOccupancy(ReadOnlySpan<GeneratedTile> tiles)
        {
            int2 min = tiles[0].Position;
            int2 max = tiles[0].Position;

            for (int i = 1; i < tiles.Length; i++)
            {
                min = math.min(min, tiles[i].Position);
                max = math.max(max, tiles[i].Position);
            }

            // The maze is sealed by walls on every side, so its bounding box
            // is the whole playable area and there is no need to know the resolution
            _min = min;
            _width = max.x - min.x + 1;
            _height = max.y - min.y + 1;
            _cells = new NativeArray<byte>(_width * _height, Allocator.Temp);

            for (int i = 0; i < tiles.Length; i++)
            {
                int2 local = tiles[i].Position - _min;
                _cells[Index(local.x, local.y)] = Wall;
            }

            int open = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == Free) open++;
            }

            OpenArea = open;
        }

        public bool TryGetRandomOpenTile(ref Random random, out int2 tile)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                int x = random.NextInt(0, _width);
                int y = random.NextInt(0, _height);
                if (_cells[Index(x, y)] != Free) continue;

                tile = _min + new int2(x, y);
                return true;
            }

            tile = default;
            return false;
        }

        public bool CanPlace(int2 origin, out int wallSides)
        {
            wallSides = 0;
            int2 local = origin - _min;

            if (IsInBounds(local) == false) return false;
            if (IsFootprintFree(local) == false) return false;
            if (HasNarrowGap(local)) return false;

            wallSides = CountWallSides(local);
            return wallSides > 0 || IsSupported(local);
        }

        // Rare and epic crates must not touch another crate, a neighbor would
        // turn them into part of a cluster. Walls and corners don't matter for them
        public bool CanPlaceAlone(int2 origin)
        {
            int2 local = origin - _min;

            if (IsInBounds(local) == false) return false;
            if (IsFootprintFree(local) == false) return false;
            if (HasNarrowGap(local)) return false;

            return TouchesCrate(local) == false;
        }

        // Callers have to check CanPlace first, placing is not validated a second time
        public void PlaceCrate(int2 origin)
        {
            int2 local = origin - _min;

            for (int dy = 0; dy < CrateSize; dy++)
            {
                for (int dx = 0; dx < CrateSize; dx++)
                {
                    _cells[Index(local.x + dx, local.y + dy)] = Crate;
                }
            }
        }

        // Clusters that end up with a single crate are thrown away,
        // which needs a way to undo everything they placed
        public void Checkpoint()
        {
            if (_checkpoint.IsCreated == false)
            {
                _checkpoint = new NativeArray<byte>(_cells.Length, Allocator.Temp);
            }

            _checkpoint.CopyFrom(_cells);
        }

        public void Rollback()
        {
            _cells.CopyFrom(_checkpoint);
        }

        public void Dispose()
        {
            if (_checkpoint.IsCreated) _checkpoint.Dispose();
            if (_cells.IsCreated) _cells.Dispose();
        }

        private int Index(int x, int y)
        {
            return y * _width + x;
        }

        // Out of bounds counts as wall so none of the neighbor checks need bounds handling
        private byte CellAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return Wall;

            return _cells[Index(x, y)];
        }

        private bool IsInBounds(int2 local)
        {
            if (local.x < 0 || local.y < 0) return false;

            return local.x + CrateSize <= _width && local.y + CrateSize <= _height;
        }

        private bool TouchesCrate(int2 local)
        {
            for (int i = 0; i < CrateSize; i++)
            {
                if (CellAt(local.x - 1, local.y + i) == Crate) return true;
                if (CellAt(local.x + CrateSize, local.y + i) == Crate) return true;
                if (CellAt(local.x + i, local.y - 1) == Crate) return true;
                if (CellAt(local.x + i, local.y + CrateSize) == Crate) return true;
            }

            return false;
        }

        private bool IsFootprintFree(int2 local)
        {
            for (int dy = 0; dy < CrateSize; dy++)
            {
                for (int dx = 0; dx < CrateSize; dx++)
                {
                    if (_cells[Index(local.x + dx, local.y + dy)] != Free) return false;
                }
            }

            return true;
        }

        private int CountWallSides(int2 local)
        {
            bool left = false;
            bool right = false;
            bool below = false;
            bool above = false;

            for (int i = 0; i < CrateSize; i++)
            {
                left |= CellAt(local.x - 1, local.y + i) == Wall;
                right |= CellAt(local.x + CrateSize, local.y + i) == Wall;
                below |= CellAt(local.x + i, local.y - 1) == Wall;
                above |= CellAt(local.x + i, local.y + CrateSize) == Wall;
            }

            return (left ? 1 : 0) + (right ? 1 : 0) + (below ? 1 : 0) + (above ? 1 : 0);
        }

        private bool HasNarrowGap(int2 local)
        {
            for (int i = 0; i < CrateSize; i++)
            {
                if (IsSlit(local.x - 1, local.y + i, -1, 0)) return true;
                if (IsSlit(local.x + CrateSize, local.y + i, 1, 0)) return true;
                if (IsSlit(local.x + i, local.y - 1, 0, -1)) return true;
                if (IsSlit(local.x + i, local.y + CrateSize, 0, 1)) return true;
            }

            return false;
        }

        // A 1-tile gap is too narrow to be useful and looks like a mistake next to
        // crates that are otherwise perfectly flush
        private bool IsSlit(int x, int y, int dx, int dy)
        {
            return CellAt(x, y) == Free && CellAt(x + dx, y + dy) != Free;
        }

        // Lets clusters grow thicker than one row, but a crate sticking out past
        // the end of a wall has no run leading to a wall and stays rejected
        private bool IsSupported(int2 local)
        {
            for (int i = 0; i < CrateSize; i++)
            {
                if (RunEndsAtWall(local.x - 1, local.y + i, -1, 0)) return true;
                if (RunEndsAtWall(local.x + CrateSize, local.y + i, 1, 0)) return true;
                if (RunEndsAtWall(local.x + i, local.y - 1, 0, -1)) return true;
                if (RunEndsAtWall(local.x + i, local.y + CrateSize, 0, 1)) return true;
            }

            return false;
        }

        private bool RunEndsAtWall(int x, int y, int dx, int dy)
        {
            while (CellAt(x, y) == Crate)
            {
                x += dx;
                y += dy;
            }

            return CellAt(x, y) == Wall;
        }
    }
}
