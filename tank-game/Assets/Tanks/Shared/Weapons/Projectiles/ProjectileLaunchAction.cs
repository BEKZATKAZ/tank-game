using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Weapons.Projectiles
{
    public class ProjectileLaunchAction : IUseAction
    {
        public ProjectileLaunchAction(
            Required<IProjectilePoolService> pool,
            Required<IProjectileConfig> config,
            ITransform muzzlePoint)
        {
            _pool = pool.Value;
            _config = config.Value;
            _muzzlePoint = muzzlePoint;
            _pool.TryInitializePool(_config);
        }

        private readonly IProjectileConfig _config;
        private readonly ITransform _muzzlePoint;
        private readonly IProjectilePoolService _pool;

        public void Use(IWeaponOwner owner, ShotId id)
        {
            ProjectileId projectileId = new ProjectileId(id, 1);

            if (_pool.TryRent(_config, projectileId, out IProjectile projectile) == false)
            {
                return;
            }

            projectile.Transform.WorldPosition = _muzzlePoint.WorldPosition;
            projectile.Transform.WorldRotation = _muzzlePoint.WorldRotation;
            projectile.Launch(owner);
        }
    }
}
