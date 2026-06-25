using System;

namespace Tanks.Shared.Generation
{
    [Flags]
    public enum DirectionFlags
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8
    }
}
