using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public class MockProjectileConfig : IProjectileConfig
    {
        public float Speed { get; set; }
        public float LifeTime { get; set; }
        public ushort MaxRicochetCount { get; set; }

        public IProjectileBehaviour Create()
        {
            ProjectileBehaviour projectile =  new GameObject("projectile")
                .AddComponent<ProjectileBehaviour>();

            projectile.Initialize(this, default);
            return projectile;
        }
    }
}
