using NaughtyAttributes;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Data.Actions;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles
{
    public sealed class WeaponActionModule : ProjectileModuleBehaviourBase
    {
        [SerializeField]
        private ProjectileModuleCallbackFlags _launchOn;

        [Required]
        [Expandable]
        [SerializeField]
        private UseActionConfig _config;

        private IUseAction _action;

        public void Setup(ProjectileModuleCallbackFlags flags, IUseActionConfig config)
        {
            _launchOn = flags;
            _action ??= config.Create(Transform);
        }

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            if (_launchOn.HasFlag(ProjectileModuleCallbackFlags.HitEntity) == false) return;
            Launch();
        }

        protected override void OnProjectileDone()
        {
            if (_launchOn.HasFlag(ProjectileModuleCallbackFlags.ProjectileDone) == false) return;
            Launch();
        }

        protected override WallHitOutput OnHitWall(float2 point, float2 normal)
        {
            if (_launchOn.HasFlag(ProjectileModuleCallbackFlags.HitWall)) Launch();
            return base.OnHitWall(point, normal);
        }

        private void Launch()
        {
            ShotId shotId = Id.ShotId;
            if (_action is null) Setup(_launchOn, _config);
            _action.Use(Projectile.Owner, new ShotId(shotId.Value + 1));
        }
    }
}
