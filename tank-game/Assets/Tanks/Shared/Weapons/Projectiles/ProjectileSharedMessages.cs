using Tanks.Shared.Adapters;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct ProjectileReturned
    {
        public ProjectileId Id;
    }

    public struct ProjectileReturnRequested
    {
        public ProjectileId Id;
        public IProjectileBehaviour Instance;
    }

    public struct ProjectileReturnRequestRejected : INetworkSerializeByMemcpy
    {
        public ProjectileId Id;
        public float2 Position;
        public Angle Rotation;
    }
}
