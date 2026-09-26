using System.Runtime.InteropServices;
using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Server.Lag
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct EntitySnapshot
    {
        public EntitySnapshot(float2 position, Angle rotation, float2 size)
        {
            Position = position;
            Rotation = rotation;
            Size = size;
        }

        public readonly float2 Position;
        public readonly Angle Rotation;
        public readonly float2 Size;
    }
}
