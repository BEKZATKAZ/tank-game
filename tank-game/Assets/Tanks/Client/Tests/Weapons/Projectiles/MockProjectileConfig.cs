using Tanks.Client.Weapons;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class MockProjectileConfig : IProjectileConfig
    {
        public float Speed { get; set; }
        public float LifeTime { get; set; }
        public ushort MaxRicochetCount { get; set; }

        public IProjectile Create() => new MockProjectile(this);
    }

    public class MockProjectile : IProjectile
    {
        public MockProjectile(IProjectileConfig config)
        {
            Config = config;
            _mover = new ProjectileMover();
        }

        public ITransform Transform { get; } = new ProjectileTransform(worldScale: 0.5f);
        public IProjectileConfig Config { get; }

        private readonly ProjectileMover _mover;

        public void Launch(IWeaponHolderBehaviour owner)
        {
            throw new System.NotImplementedException();
        }

        public void Reconcile(ProjectileSnapshotFromServer snapshot)
        {
            throw new System.NotImplementedException();
        }

        public void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime)
        {
            _mover.Simulate(callbacks, transform, Config.Speed * deltaTime);
        }
    }
}
