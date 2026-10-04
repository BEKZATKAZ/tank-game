using NaughtyAttributes;
using Tanks.Core.Helpers;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Weapons.Data.Actions
{

    [CreateAssetMenu(
        fileName = "UseAction",
        menuName = "Scriptable Objects/Weapons/Actions/Projectile Launch")]
    public class ProjectileLaunchActionConfig : UseActionConfig
    {
        [Required]
        [SerializeField]
        private ProjectileBehaviour _prefab;

        [Range(1, 50)]
        [SerializeField]
        private ushort _projectileCount = 1;

        [Range(0, 180)]
        [SerializeField]
        private float _spreadDegrees;

        public ProjectileBehaviour Prefab => _prefab;
        public ushort ProjectileCount => _projectileCount;
        public float SpreadDegrees => _spreadDegrees;

        public override IUseAction Create(ITransform muzzlePoint)
        {
            IProjectilePoolService pool = LifetimeScope
                .Find<GameplayScope>().Container
                .Resolve<IProjectilePoolService>();

            return new ProjectileLaunchAction(pool.AsRequired(), this, muzzlePoint);
        }
    }
}
