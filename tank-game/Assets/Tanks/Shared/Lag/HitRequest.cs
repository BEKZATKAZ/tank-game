using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Netcode;

namespace Tanks.Shared.Lag
{
    public struct HitRequest : INetworkSerializeByMemcpy
    {
        public EntityId TargetId;
        public ProjectileId ProjectileId;
        public int ClientTick;
    }
}
