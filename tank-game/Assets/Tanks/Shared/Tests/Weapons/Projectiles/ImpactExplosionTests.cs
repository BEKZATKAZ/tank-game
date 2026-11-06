using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Weapons.Projectiles
{
    public sealed class ImpactExplosionTests
    {
        private ImpactExplosionModule _explosion;
        private IDamageService _damage;
        private HealthBehaviour _target;
        private GameObject _wall;

        [SetUp]
        public void Setup()
        {
            _explosion = new GameObject().AddComponent<ImpactExplosionModule>();
            _explosion.Projectile = Substitute.For<IProjectileBehaviour>();
            _explosion.Projectile.Transform.WorldPosition.Returns(new float2(8000, 8000));

            _damage = Substitute.For<IDamageService>();
            _explosion.Construct(_damage);
            _explosion.Effects.Add(EffectType.Burning, 5);

            GameObject target = new("explosion-target", typeof(BoxCollider2D), typeof(HealthBehaviour))
            {
                layer = 3
            };

            target.transform.position = new Vector3(8002, 8000, 0);
            _target = target.GetComponent<HealthBehaviour>();

            typeof(HealthBehaviour)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_target, null);

            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_explosion.gameObject);
            Object.DestroyImmediate(_target.gameObject);
            if (_wall) Object.DestroyImmediate(_wall);
        }

        [Test]
        public void ProjectileDone_ExplodesOnceAndResetsOnNextLaunch()
        {
            _explosion.Handle(default);
            _explosion.ProjectileDone();
            _explosion.ProjectileDone();

            _damage.Received(1).Damage(76,
                _target.Entity, _explosion.Projectile.Owner.Entity,
                HealthEventId.Create(_explosion.GetType(), _explosion.Id));

            _explosion.Handle(default);
            _explosion.ProjectileDone();

            _damage.Received(2).Damage(76,
                _target.Entity, _explosion.Projectile.Owner.Entity,
                HealthEventId.Create(_explosion.GetType(), _explosion.Id));
        }

        [Test]
        public void Wall_BlocksBlastDamage()
        {
            _wall = new GameObject("blast-wall", typeof(BoxCollider2D));
            _wall.transform.position = new Vector3(8001, 8000, 0);
            Physics2D.SyncTransforms();

            _explosion.ProjectileDone();

            _damage.DidNotReceiveWithAnyArgs().Damage(default, default, default, default, default);
        }

        [Test]
        public void Contacts_DoNotExplodeBeforeProjectileDone()
        {
            _explosion.HitEntity(_target.Entity, null);
            _explosion.HitWall(new float2(8000, 8000), new float2(1, 0));

            _damage.DidNotReceiveWithAnyArgs().Damage(default, default, default, default, default);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Burning_UsesShootersAuthorityForPooledProjectiles(bool server)
        {
            EffectOwnerBehaviour effects = _target.gameObject.AddComponent<EffectOwnerBehaviour>();
            _explosion.Projectile.Owner.IsServer.Returns(server);

            _explosion.ProjectileDone();

            Assert.That(effects.Effects.ContainsKey(EffectType.Burning), Is.EqualTo(server));
            if (server) Assert.That(effects.Effects[EffectType.Burning].Duration, Is.EqualTo(3).Within(0.001f));
        }

        [Test]
        public void Burning_AppliesBeforeDamageCanDespawnTheTarget()
        {
            EffectOwnerBehaviour effects = _target.gameObject.AddComponent<EffectOwnerBehaviour>();
            _explosion.Projectile.Owner.IsServer.Returns(true);

            bool burningWhenDamaged = false;

            _damage.When(x => x.Damage(Arg.Any<uint>(), Arg.Any<Shared.Entities.IEntityBehaviour>(),
                Arg.Any<Shared.Entities.IEntityBehaviour>(), Arg.Any<HealthEventId>(), Arg.Any<bool>()))
                .Do(_ => burningWhenDamaged = effects.Effects.ContainsKey(EffectType.Burning));

            _explosion.ProjectileDone();
            Assert.That(burningWhenDamaged, Is.True);
        }

        [Test]
        public void Effects_DictionaryScalesEveryConfiguredDuration()
        {
            EffectOwnerBehaviour effects = _target.gameObject.AddComponent<EffectOwnerBehaviour>();
            _explosion.Projectile.Owner.IsServer.Returns(true);

            _explosion.Effects.Add(EffectType.Healing, 10);
            _explosion.ProjectileDone();

            Assert.That(effects.Effects[EffectType.Burning].Duration, Is.EqualTo(3).Within(0.001f));
            Assert.That(effects.Effects[EffectType.Healing].Duration, Is.EqualTo(6).Within(0.001f));
        }

        [Test]
        public void RadialModule_UsesSameDamageOnProjectileDone()
        {
            GameObject radialObject = new("radial-test");
            radialObject.transform.position = new Vector3(8000, 8000, 0);
            RadialDamageModule radial = radialObject.AddComponent<RadialDamageModule>();
            typeof(ProjectileModuleBehaviourBase).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(radial, null);
            radial.Projectile = _explosion.Projectile;
            radial.Radius = _explosion.Radius;
            radial.NearDamage = _explosion.NearDamage;
            radial.FarDamage = _explosion.FarDamage;
            radial.Construct(_damage);

            try
            {
                radial.HitEntity(_target.Entity, null);
                _damage.DidNotReceiveWithAnyArgs().Damage(default, default);
                radial.ProjectileDone();

                _damage.Received(1).Damage(76, _target.Entity, radial.Projectile.Owner.Entity,
                    HealthEventId.Create(radial.GetType(), radial.Id));
            }
            finally { Object.DestroyImmediate(radialObject); }
        }
    }
}
