using System;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct LaunchCallbacks
    {
        public Action<float2, float2> OnRicochet;
        public Action<IHealthBehaviour> OnDamageableHit;
    }

    public interface IProjectile
    {
        ITransform Transform { get; }
        IProjectileConfig Config { get; }

        void Launch(IWeaponHolderBehaviour owner);
        void Reconcile(ProjectileSnapshotFromServer snapshot);
        void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime);
    }
}
