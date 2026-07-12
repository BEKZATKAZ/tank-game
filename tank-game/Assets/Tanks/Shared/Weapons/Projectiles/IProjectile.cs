using System;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct LaunchCallbacks
    {
        public Action<float2> OnRicochet;
        public Action<IDamageable> OnDamageableHit;
    }

    public interface IProjectile
    {
        ITransform Transform { get; }
        IProjectileConfig Config { get; }

        void Launch();
        void Reconcile(ProjectileSnapshotFromServer snapshot);
        void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime);
    }
}
