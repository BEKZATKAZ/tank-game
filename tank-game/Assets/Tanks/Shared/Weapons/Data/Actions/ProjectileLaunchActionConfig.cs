using NaughtyAttributes;
using Tanks.Core.Helpers;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
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
        [Expandable]
        [Required]
        [SerializeField]
        private ProjectileConfig _projectile;

        public override IUseAction Create(ITransform muzzlePoint)
        {
            IProjectilePoolService pool = LifetimeScope
                .Find<LifetimeScope>().Container
                .Resolve<IProjectilePoolService>();

            return new ProjectileLaunchAction(pool.AsRequired(), _projectile, muzzlePoint);
        }
    }
}
