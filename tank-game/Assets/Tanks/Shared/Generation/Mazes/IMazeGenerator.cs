using System;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.Shared.Generation.Mazes
{
    [Flags]
    public enum WallFlags : byte
    {
        None = 0,
        Left = 1,
        Bottom = 2
    }

    public struct MazeCell : IEquatable<MazeCell>
    {
        public MazeCell(int2 position)
        {
            Position = position;
            IsVisited = false;
            Flags = WallFlags.Left | WallFlags.Bottom;
        }

        public readonly bool WallLeft => Flags.HasFlag(WallFlags.Left);
        public readonly bool WallBottom => Flags.HasFlag(WallFlags.Bottom);

        public bool IsVisited { get; private set; }
        public WallFlags Flags { get; private set; }
        public readonly int2 Position { get; }

        public void Visit() => IsVisited = true;

        public void SetWallLeft(bool active)
        {
            if (active) Flags |= WallFlags.Left;
            else Flags &= ~WallFlags.Left;
        }

        public void SetWallBottom(bool active)
        {
            if (active) Flags |= WallFlags.Bottom;
            else Flags &= ~WallFlags.Bottom;
        }

        public override readonly int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override readonly bool Equals(object obj)
        {
            return obj is MazeCell other && Equals(other);
        }

        public readonly bool Equals(MazeCell other)
        {
            return IsVisited == other.IsVisited
                && Flags == other.Flags
                && math.all(Position == other.Position);
        }
    }

    public interface IMazeGenerator
    {
        TemporaryArrayResult<MazeCell> Generate(uint seed, int2 resolution);
    }
}
