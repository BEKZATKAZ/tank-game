using System;
using Tanks.Shared.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public enum WallHitOutput
    {
        None,
        AllowRicochet
    }

    [Flags]
    public enum ProjectileModuleCallbackFlags
    {
        None = 0,
        HitWall = 1,
        HitEntity = 2,
        ProjectileDone = 4
    }

    public struct ProjectileDone { }
    public struct ProjectileHitEntity
    {
        public IEntityBehaviour Target;
        public IEntityBehaviour From;
    }

    public abstract class ProjectileModuleBehaviourBase : NetworkBehaviourExtended
    {
        public IProjectileBehaviour Projectile { get; set; }
        public ProjectileId Id => Projectile.Id;

        protected virtual WallHitOutput OnHitWall(float2 point, float2 normal)
        {
            return WallHitOutput.AllowRicochet;
        }

        protected virtual void OnProjectileDone() { }
        protected virtual void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from) { }

        public WallHitOutput HitWall(float2 point, float2 normal)
        {
            return OnHitWall(point, normal);
        }

        public virtual void HitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            EmitMessage(new ProjectileHitEntity { Target = target, From = from });
            OnHitEntity(target, from);
        }

        public void ProjectileDone()
        {
            EmitMessage(new ProjectileDone());
            OnProjectileDone();
        }

        protected override void Awake()
        {
            base.Awake();
            Projectile = GetComponentInParent<ProjectileBehaviour>();
        }
    }
}
