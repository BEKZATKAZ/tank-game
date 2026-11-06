using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles.Modules;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles;
using UnityEditor;
using UnityEngine;
using EntityId = Tanks.Shared.Entities.EntityId;

namespace Tanks.Server.Tests.Weapons.Projectiles
{
    public class GiveEffectModuleTests
    {
        private GiveEffectModule _module;

        [SetUp]
        public void Setup()
        {
            _module = new GameObject().AddComponent<GiveEffectModule>();
            _module.Projectile = Substitute.For<IProjectileBehaviour>();
            _module.Projectile.Owner.IsServer.Returns(true);
            _module.Projectile.Name.Returns("test-projectile");
        }

        [TearDown]
        public void TearDown()
        {
            if (_module) Object.DestroyImmediate(_module.gameObject);
        }

        [TestCase(EffectType.Burning, 5)]
        [TestCase(EffectType.Frozen, 3)]
        public void HitEntity_AppliesConfiguredEffectWithSource(EffectType type, float duration)
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();
            _module.Effects.Add(type, duration);

            _module.HitEntity(target, from);

            owner.Received(1).ApplyEffect(type, duration, "test-projectile", from, false);
        }

        [Test]
        public void HitEntity_NoConfiguredEffects_DoesNotApplyEffect()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();

            _module.HitEntity(target, null);

            owner.DidNotReceiveWithAnyArgs().ApplyEffect(default, default, default, default);
        }

        [Test]
        public void HitEntity_TargetWithoutEffectOwner_DoesNothing()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            target.GetBehaviour<IEffectOwnerBehaviour>().Returns((IEffectOwnerBehaviour)null);
            _module.Effects.Add(EffectType.Frozen, 3);

            Assert.DoesNotThrow(() => _module.HitEntity(target, null));
        }

        [Test]
        public void HitEntity_AppliesEveryConfiguredEffect()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();
            _module.Effects.Add(EffectType.Burning, 5);
            _module.Effects.Add(EffectType.Frozen, 3);

            _module.HitEntity(target, null);

            owner.Received(1).ApplyEffect(EffectType.Burning, 5, "test-projectile", null, false);
            owner.Received(1).ApplyEffect(EffectType.Frozen, 3, "test-projectile", null, false);
        }

        [Test]
        public void ClientHit_DoesNotApplyEffects()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            _module.Projectile.Owner.IsServer.Returns(false);
            _module.Effects.Add(EffectType.Burning, 5);

            _module.HitEntity(target, null);

            target.GetBehaviour<IEffectOwnerBehaviour>().DidNotReceiveWithAnyArgs()
                .ApplyEffect(default, default, default, default);
        }

        [Test]
        public void IgnitedBallsPrefab_RestartsItsBurningEffect()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Tanks/Prefabs/Projectiles/Projectile_IgnitedBalls.prefab");
            GiveEffectModule module = prefab.GetComponentInChildren<GiveEffectModule>(true);

            Assert.That(module.RestartEffects, Is.True);
            Assert.That(module.Effects.ContainsKey(EffectType.Burning), Is.True);
        }

        [Test]
        public void IgnitedHits_AcrossPooledInstancesRefreshOnlyTheirAttackerAndWeapon()
        {
            GameObject targetObject = new();
            GiveEffectModule otherModule = new GameObject().AddComponent<GiveEffectModule>();

            try
            {
                EffectOwnerBehaviour effects = targetObject.AddComponent<EffectOwnerBehaviour>();
                IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
                target.GetBehaviour<IEffectOwnerBehaviour>().Returns(effects);
                IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
                IEntityBehaviour second = Substitute.For<IEntityBehaviour>();
                first.Id.Returns(new EntityId(1));
                second.Id.Returns(new EntityId(2));
                _module.RestartEffects = true;
                _module.Effects.Add(EffectType.Burning, 5);
                otherModule.Projectile = _module.Projectile;
                otherModule.RestartEffects = true;
                otherModule.Effects.Add(EffectType.Burning, 5);
                EffectId item = effects.ApplyEffect(EffectType.Burning, 20, "item", first);

                _module.HitEntity(target, first);
                effects.OnTick(2, 1);
                otherModule.HitEntity(target, first);

                Assert.That(effects.Entries.Count, Is.EqualTo(2));
                Assert.That(effects.Entries[item].Duration, Is.EqualTo(20));
                Assert.That(effects.Entries[item].StartTick, Is.Zero);

                // A pooled instance firing for another attacker must create a separate entry.
                otherModule.HitEntity(target, second);
                Assert.That(effects.Entries.Count, Is.EqualTo(3));

                effects.OnTick(6, 1);
                Assert.That(effects.Entries.Count, Is.EqualTo(3));
                effects.OnTick(7, 1);
                Assert.That(effects.Entries.Count, Is.EqualTo(1));
                Assert.That(effects.HasEffect(item), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(otherModule.gameObject);
            }
        }
    }
}
