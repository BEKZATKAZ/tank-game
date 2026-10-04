using NSubstitute;
using NUnit.Framework;
using System.Collections;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tanks.Shared.Tests.Runtime.Weapons
{
    public sealed class TestModule : ProjectileModuleBehaviourBase
    {
        public int ProjectileDoneReceived { get; private set; }
        public int HitEntityReceived { get; private set; }
        public int HitWallReceived { get; private set; }

        public IEntityBehaviour LastTarget { get; private set; }
        public IEntityBehaviour LastFrom { get; private set; }

        public float2 LastPoint { get; private set; }
        public float2 LastNormal { get; private set; }

        protected override void OnProjectileDone()
        {
            ProjectileDoneReceived++;
        }

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            HitEntityReceived++;
            LastTarget = target;
            LastFrom = from;
        }

        protected override WallHitOutput OnHitWall(float2 point, float2 normal)
        {
            HitWallReceived++;
            LastPoint = point;
            LastNormal = normal;
            return base.OnHitWall(point, normal);
        }
    }

    public class ProjectileModuleTests
    {
        private ProjectileBehaviour _behaviour;
        private TestModule _module;

        [SetUp]
        public void Setup()
        {
            _behaviour = new GameObject(
                "projectile", typeof(TestModule), typeof(ProjectileModuleList))
                .AddComponent<ProjectileBehaviour>();

            _module = _behaviour.GetBehaviour<TestModule>();
        }

        [UnityTest]
        public IEnumerator OnDone_RunsOnDisable_CallsModule()
        {
            _behaviour.gameObject.SetActive(false);
            yield return null;

            // Not initialized yet
            Assert.That(_module.ProjectileDoneReceived, Is.EqualTo(0));
            Assert.That(_module.HitEntityReceived, Is.EqualTo(0));
            Assert.That(_module.HitWallReceived, Is.EqualTo(0));

            _behaviour.gameObject.SetActive(true);
            yield return null;

            _behaviour.Launch(Substitute.For<IWeaponHolderBehaviour>(), new ProjectileId(new(1), 1));
            _behaviour.gameObject.SetActive(false);
            yield return null;

            // Initialized
            Assert.That(_module.ProjectileDoneReceived, Is.EqualTo(1));
            Assert.That(_module.HitEntityReceived, Is.EqualTo(0));
            Assert.That(_module.HitWallReceived, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OnHitWall_RunsOnRicochet_CallsModule()
        {
            yield return null;
            _behaviour.OnRicochet(new float2(1, 4), new float2(1, 0));

            Assert.That(_module.HitWallReceived, Is.EqualTo(1));
            Assert.That(_module.LastPoint, Is.EqualTo(new float2(1, 4)));
            Assert.That(_module.LastNormal, Is.EqualTo(new float2(1, 0)));
        }

        [UnityTest]
        public IEnumerator OnHitEntity_RunsOnDamageableHit_CallsModule()
        {
            IHealthBehaviour target = Substitute.For<IHealthBehaviour>();
            IWeaponHolderBehaviour owner = Substitute.For<IWeaponHolderBehaviour>();

            _behaviour.Launch(owner, default);
            yield return null;
;
            _behaviour.OnDamageableHit(target);

            Assert.That(_module.HitEntityReceived, Is.EqualTo(1));
            Assert.That(_module.LastTarget, Is.EqualTo(target.Entity));

            Assert.That(_module.LastFrom, Is.EqualTo(_behaviour.Owner.Entity));
            Assert.That(_behaviour.Owner, Is.EqualTo(owner));
        }
    }
}
