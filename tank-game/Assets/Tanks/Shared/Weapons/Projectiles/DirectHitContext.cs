using Tanks.Shared.Entities;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct DirectHitContext
    {
        public ProjectileId ProjectileId;
        public IEntityBehaviour Target;
        public float2 Direction;
        public float DamageMultiplier;
    }
}
