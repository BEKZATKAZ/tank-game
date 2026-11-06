using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles.Modules;
using Tanks.Shared;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests.Weapons.Projectiles
{
    public sealed class AreaEffectModuleTests
    {
        private AreaEffectModule _module;
        private HealthBehaviour _health;
        private EffectOwnerBehaviour _effects;
        private GameObject _wall;
        private readonly float2 _center = new(9000, 9000);

        [SetUp]
        public void Setup()
        {
            _module = new GameObject().AddComponent<AreaEffectModule>();
            _module.Projectile = Substitute.For<IProjectileBehaviour>();
            _module.Projectile.Owner.IsServer.Returns(true);
            _module.Projectile.Transform.WorldPosition.Returns(_center);

            _module.Effects.Add(EffectType.Flashed, 5);
            _module.Construct();

            GameObject target = new("flash-target", typeof(BoxCollider2D), typeof(HealthBehaviour))
            {
                layer = 3
            };

            target.transform.position = new Vector3(9005, 9000, 0);
            _health = target.GetComponent<HealthBehaviour>();

            typeof(HealthBehaviour)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_health, null);

            typeof(NetworkBehaviourExtended)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target.GetComponent<EntityBehaviour>(), null);

            _effects = target.AddComponent<EffectOwnerBehaviour>();
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_module.gameObject);
            Object.DestroyImmediate(_health.gameObject);
            if (_wall) Object.DestroyImmediate(_wall);
        }

        [TestCase(0, 5)]
        [TestCase(5, 2.5f)]
        [TestCase(9, 0.5f)]
        public void Impact_ScalesDurationByDistance(float distance, float duration)
        {
            _health.transform.position = (Vector2)(_center + new float2(distance, 0));

            Physics2D.SyncTransforms();
            _module.ProjectileDone();

            Assert.That(_effects.Effects[EffectType.Flashed].Duration, Is.EqualTo(duration).Within(0.001f));
            Assert.That(_effects.Effects[EffectType.Flashed].From, Is.SameAs(_module.Projectile.Owner.Entity));
            Assert.That(_health.ServerState.Value.Current, Is.EqualTo(100));
        }

        [TestCase(10)]
        [TestCase(11)]
        public void Impact_AtOrBeyondRadiusDoesNotFlash(float distance)
        {
            _health.transform.position = (Vector2)(_center + new float2(distance, 0));
            Physics2D.SyncTransforms();

            _module.ProjectileDone();

            Assert.That(_effects.Effects, Is.Empty);
        }

        [Test]
        public void Wall_BlocksFlash()
        {
            _wall = new GameObject("flash-wall", typeof(BoxCollider2D));
            _wall.transform.position = new Vector3(9002, 9000, 0);
            Physics2D.SyncTransforms();

            _module.ProjectileDone();

            Assert.That(_effects.Effects, Is.Empty);
        }

        [Test]
        public void Trigger_DoesNotBlockFlash()
        {
            _wall = new GameObject("flash-trigger", typeof(BoxCollider2D));
            _wall.GetComponent<BoxCollider2D>().isTrigger = true;
            _wall.transform.position = new Vector3(9002, 9000, 0);
            Physics2D.SyncTransforms();
            bool previous = Physics2D.queriesHitTriggers;

            try
            {
                Physics2D.queriesHitTriggers = true;
                _module.ProjectileDone();
                Assert.That(_effects.Effects.ContainsKey(EffectType.Flashed), Is.True);
            }
            finally { Physics2D.queriesHitTriggers = previous; }
        }

        [Test]
        public void WallImpact_FlashesVisibleTargetsOnImpactSide()
        {
            _wall = new GameObject("impact-wall", typeof(BoxCollider2D));
            _wall.transform.position = new Vector3(8999.5f, 9000, 0);
            Physics2D.SyncTransforms();

            _module.Projectile.Transform.WorldPosition.Returns(_center + new float2(0.01f, 0));
            _module.ProjectileDone();

            Assert.That(_effects.Effects.ContainsKey(EffectType.Flashed), Is.True);
        }

        [Test]
        public void Impact_IncludesShooter()
        {
            _module.Projectile.Owner.Entity.Returns(_health.Entity);

            _module.ProjectileDone();

            Assert.That(_effects.Effects[EffectType.Flashed].From, Is.SameAs(_health.Entity));
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        [TestCase(1, 1)]
        public void WallCompletion_FlashesAndBurnsShooterWithoutReachingThroughWall(int x, int y)
        {
            ProjectileBehaviour projectile = new GameObject().AddComponent<ProjectileBehaviour>();
            typeof(ProjectileBehaviour).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(projectile, null);
            typeof(ProjectileBehaviour).GetField("_maxRicochetCount", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(projectile, (byte)0);
            ImpactExplosionModule explosion = projectile.gameObject.AddComponent<ImpactExplosionModule>();
            explosion.Projectile = _module.Projectile;
            explosion.Construct(Substitute.For<IDamageService>());
            explosion.Effects.Add(EffectType.Burning, 5);
            _module.Projectile.Owner.Entity.Returns(_health.Entity);
            float2 normal = math.normalize(new float2(x, y));
            _wall = new GameObject("contact-wall", typeof(BoxCollider2D));
            _wall.transform.position = (Vector2)(_center - new float2(x, y) * 0.5f);
            _health.transform.position = (Vector2)(_center + normal * 2);
            Physics2D.SyncTransforms();
            bool previous = Physics2D.queriesStartInColliders;

            try
            {
                Physics2D.queriesStartInColliders = true;
                projectile.OnRicochet(_center, normal);
                float2 position = projectile.Transform.WorldPosition;
                Assert.That(math.distance(position, _center + normal * 0.01f), Is.LessThan(0.001f));
                _module.Projectile.Transform.WorldPosition.Returns(position);
                _module.ProjectileDone();
                explosion.ProjectileDone();

                Assert.That(_effects.Effects.ContainsKey(EffectType.Flashed), Is.True);
                Assert.That(_effects.Effects.ContainsKey(EffectType.Burning), Is.True);

                _effects.OnTick(1000, 1);
                _health.transform.position = (Vector2)(_center - normal * 2);
                Physics2D.SyncTransforms();
                _module.Handle(default);
                explosion.Handle(default);
                _module.ProjectileDone();
                explosion.ProjectileDone();

                Assert.That(_effects.Effects, Is.Empty);
            }
            finally
            {
                Physics2D.queriesStartInColliders = previous;
                Object.DestroyImmediate(projectile.gameObject);
            }
        }

        [Test]
        public void Relaunch_RestartsFlashInsteadOfStacking()
        {
            _module.ProjectileDone();
            _effects.OnTick(10, 0.1f);
            _module.Handle(default);

            _module.ProjectileDone();

            Assert.That(_effects.Effects[EffectType.Flashed].StartTick, Is.EqualTo(10));
            Assert.That(_effects.Effects[EffectType.Flashed].Duration, Is.EqualTo(2.5f));
        }

        [Test]
        public void Impact_MultipleCollidersAndCallbacksApplyOnlyOnce()
        {
            _health.gameObject.AddComponent<CircleCollider2D>();
            _module.RestartEffects = false;
            Physics2D.SyncTransforms();

            _module.ProjectileDone();
            _module.HitWall(_center, new float2(1, 0));

            Assert.That(_effects.Effects[EffectType.Flashed].Duration, Is.EqualTo(2.5f));
        }

        [Test]
        public void ClientImpact_DoesNotApplyEffects()
        {
            _module.Projectile.Owner.IsServer.Returns(false);

            _module.ProjectileDone();

            Assert.That(_effects.Effects, Is.Empty);
        }

        [Test]
        public void Contacts_DoNotFlashBeforeCompletion()
        {
            _module.HitEntity(_health.Entity, null);
            _module.HitWall(_center, new float2(1, 0));

            Assert.That(_effects.Effects, Is.Empty);
        }

        [Test]
        public void Impact_DeadTargetIsIgnored()
        {
            ((BindableProperty<HealthState>)_health.ServerState).Value = new HealthState(0, 100);

            _module.ProjectileDone();

            Assert.That(_effects.Effects, Is.Empty);
        }

        [Test]
        public void Configuration_CanDisableFalloffAndApplyMultipleEffects()
        {
            _module.DistanceFalloff = false;
            _module.Effects.Add(EffectType.Healing, 2);

            _module.ProjectileDone();

            Assert.That(_effects.Effects[EffectType.Flashed].Duration, Is.EqualTo(5));
            Assert.That(_effects.Effects[EffectType.Healing].Duration, Is.EqualTo(2));
        }
    }
}
