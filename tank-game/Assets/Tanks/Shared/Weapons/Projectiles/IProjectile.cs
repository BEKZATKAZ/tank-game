using System;
using Tanks.Shared.Adapters;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct LaunchSettings
    {
        public float Speed;
        public float LifeTimeSeconds;
        public int RicochetCount;

        public Action<IProjectile> OnDone;
    }

    public interface IProjectile
    {
        ITransform Transform { get; }

        void Launch(LaunchSettings settings);
    }
}