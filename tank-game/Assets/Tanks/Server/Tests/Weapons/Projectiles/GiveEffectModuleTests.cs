using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles.Modules;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

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

            owner.Received(1).AddEffect(type, duration, from);
        }

        [Test]
        public void HitEntity_NoConfiguredEffects_DoesNotApplyEffect()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();

            _module.HitEntity(target, null);

            owner.DidNotReceiveWithAnyArgs().AddEffect(default, default, default);
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

            owner.Received(1).AddEffect(EffectType.Burning, 5, null);
            owner.Received(1).AddEffect(EffectType.Frozen, 3, null);
        }
    }
}
