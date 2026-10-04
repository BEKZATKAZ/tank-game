using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data.Actions;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Server.Weapons.Projectiles
{
    public class ProjectileLaunchAction : IUseAction
    {
        public ProjectileLaunchAction(
            Required<IProjectilePoolService> pool,
            Required<ProjectileLaunchActionConfig> actionConfig,
            ITransform muzzlePoint)
        {
            _pool = pool.Value;
            _pool.TryInitializePool(actionConfig.Value.Prefab);

            _muzzlePoint = muzzlePoint;
            _actionConfig = actionConfig;
        }

        private readonly ITransform _muzzlePoint;
        private readonly IProjectilePoolService _pool;
        private readonly ProjectileLaunchActionConfig _actionConfig;

        public void Use(IWeaponHolderBehaviour owner, ShotId id)
        {
            string prefabName = _actionConfig.Prefab.Name;

            for (ushort value = 1; value <= _actionConfig.ProjectileCount; value++)
            {
                ProjectileId projectileId = new ProjectileId(id, value);

                if (_pool.TryRent(prefabName, projectileId, out var projectile) == false)
                {
                    continue;
                }

                float t = (value - 0.5f) / _actionConfig.ProjectileCount;
                float spread = _actionConfig.SpreadDegrees;

                float offset = math.lerp(-spread, spread, t);
                Angle rotation = _muzzlePoint.WorldRotation + Angle.FromDegrees(offset);

                projectile.Transform.WorldPosition = _muzzlePoint.WorldPosition;
                projectile.Transform.WorldRotation = rotation;
                projectile.Launch(owner, projectileId);
            }
        }
    }
}
