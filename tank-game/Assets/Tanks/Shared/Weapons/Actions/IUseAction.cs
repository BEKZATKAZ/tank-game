using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Shared.Weapons.Actions
{
    public interface IUseAction
    {
        void Use();
    }

    public class ProjectileLaunchAction : IUseAction
    {
        public ProjectileLaunchAction(IProjectilePoolService pool, ITransform muzzlePoint)
        {
            _pool = pool;
            _muzzlePoint = muzzlePoint;
        }

        private readonly IProjectilePoolService _pool;
        private readonly ITransform _muzzlePoint;

        public void Use()
        {
            LaunchSettings settings = new LaunchSettings
            {
                LifeTimeSeconds = 10,
                Speed = 15,
                OnDone = _pool.Release
            };

            _pool.LaunchAt(_muzzlePoint.WorldPosition, _muzzlePoint.WorldRotation, settings);
        }
    }
}
