using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public sealed class DirectHitLifecycleTests
    {
        private sealed class Counter : ItemBehaviourBase
        {
            public int Hits;
            public ProjectileId LastProjectileId;
            public override void OnDirectHit(IItemOwnerBehaviour owner, ref DirectHitContext context)
            {
                Hits++;
                LastProjectileId = context.ProjectileId;
            }
        }

        [Test]
        public void Hits_DeduplicateStackedCallbacksAndResetWhenProjectileIsReused()
        {
            ProjectileBehaviour projectile = new GameObject().AddComponent<ProjectileBehaviour>();
            typeof(ProjectileBehaviour).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(projectile, null);
            IWeaponHolderBehaviour holder = Substitute.For<IWeaponHolderBehaviour>();
            IItemOwnerBehaviour owner = holder.Entity.GetBehaviour<IItemOwnerBehaviour>();
            Counter counter = new();
            owner.GetAllBehaviours().Returns(new[] { counter, counter });
            IHealthBehaviour health = Substitute.For<IHealthBehaviour>();
            health.ServerState.Returns(new BindableProperty<HealthState>(new HealthState(100, 100)));
            health.Entity.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(1, "target", new TeamId(1)));

            try
            {
                projectile.Launch(holder, new ProjectileId(new ShotId(1), 1));
                projectile.OnDamageableHit(health);
                projectile.OnDamageableHit(health);

                Assert.That(counter.Hits, Is.EqualTo(1));
                Assert.That(projectile.DirectHitTarget, Is.SameAs(health.Entity));

                projectile.Launch(holder, new ProjectileId(new ShotId(2), 1));

                Assert.That(projectile.DirectHitTarget, Is.Null);

                projectile.OnDamageableHit(health);

                Assert.That(counter.Hits, Is.EqualTo(2));
                Assert.That(counter.LastProjectileId, Is.EqualTo(new ProjectileId(new ShotId(2), 1)));
            }
            finally { Object.DestroyImmediate(projectile.gameObject); }
        }
    }
}
