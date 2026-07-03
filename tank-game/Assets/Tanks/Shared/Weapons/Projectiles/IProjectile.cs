using System;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;

namespace Tanks.Shared.Weapons.Projectiles
{
    public delegate void DamageableHitCallback(IDamageable target, float distance);

    public struct LaunchSettings
    {
        public float Speed;
        public float LifeTimeSeconds;

        public int RicochetCount;
        public IDamage Damage;
    }

    public struct LaunchCallbacks
    {
        public DamageableHitCallback OnHit;
        public Action<IProjectile> OnDone;
    }

    public interface IProjectile
    {
        ITransform Transform { get; }

        void Launch(LaunchSettings settings, LaunchCallbacks callbacks);
    }
}