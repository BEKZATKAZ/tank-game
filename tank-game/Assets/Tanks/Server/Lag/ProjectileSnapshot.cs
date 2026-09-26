using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Tanks.Server.Lag
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ProjectileSnapshot
    {
        public ProjectileSnapshot(float2 position, float radius, float elapsed)
        {
            Position = position;
            Radius = radius;
            Elapsed = elapsed;
        }

        public readonly float2 Position;
        public readonly float Radius;
        public readonly float Elapsed;
    }
}
