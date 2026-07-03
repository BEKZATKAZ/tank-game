using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Shared.Weapons.Actions
{
    public interface IUseAction
    {
        void Use();
    }

    public class ProjectileLaunchAction : IUseAction
    {
        public ProjectileLaunchAction(
            IProjectilePoolService pool, ITransform muzzlePoint, IDamage damage)
        {
            _pool = pool;
            _muzzlePoint = muzzlePoint;
            _damage = damage;
        }

        private readonly IProjectilePoolService _pool;
        private readonly ITransform _muzzlePoint;
        private readonly IDamage _damage;

        public void Use()
        {
            LaunchSettings settings = new LaunchSettings
            {
                LifeTimeSeconds = 10,
                Speed = 15,
                RicochetCount = 10,
                Damage = _damage
            };

            _pool.LaunchAt(_muzzlePoint.WorldPosition, _muzzlePoint.WorldRotation, settings);
        }
    }
}
