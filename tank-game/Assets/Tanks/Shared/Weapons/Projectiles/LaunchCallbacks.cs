using System;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons.Projectiles
{
    public delegate void RicochetCallback(float2 position, float2 normal);

    public struct LaunchCallbacks
    {
        public RicochetCallback OnRicochet;
        public Action<IHealthBehaviour> OnDamageableHit;
    }
}
