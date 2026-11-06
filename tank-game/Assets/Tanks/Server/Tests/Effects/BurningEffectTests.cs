using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Effects;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using UnityEngine;

namespace Tanks.Server.Tests.Effects
{
    public class BurningEffectTests
    {
        private BurningEffect _effect;
        private IDamageService _damageService;
        private IEffectOwnerBehaviour _owner;
        private Dictionary<EffectType, EffectEntry> _effects;

        [SetUp]
        public void Setup()
        {
            _damageService = Substitute.For<IDamageService>();
            _owner = Substitute.For<IEffectOwnerBehaviour>();
            _effects = new Dictionary<EffectType, EffectEntry>();
            _owner.Effects.Returns(_effects);

            _effect = new BurningEffect(_damageService);
            _effect.Initialize(_owner);
        }

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(20)]
        public void Tick_BurningOnDamageTick_DealsSixDamageWithoutStats(int tick)
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            _effects[EffectType.Burning] = new EffectEntry(0, 30, from);

            _effect.Tick(tick, 0.1f);

            _damageService.Received(1).Damage(6, _owner.Entity, from,
                HealthEventId.Create(_owner.GetType(), tick), includeStats: false);
        }

        [TestCase(1)]
        [TestCase(9)]
        [TestCase(11)]
        public void Tick_BetweenDamageTicks_DoesNotDamage(int tick)
        {
            _effects[EffectType.Burning] = new EffectEntry(0, 30, null);

            _effect.Tick(tick, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public void Tick_MissingOrFrozenEffect_DoesNotDamage()
        {
            _effect.Tick(10, 0.1f);
            _effects[EffectType.Frozen] = new EffectEntry(0, 30, null);
            _effect.Tick(20, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public void Tick_ChangedSource_UsesLatestSourceAndTickEvent()
        {
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            _effects[EffectType.Burning] = new EffectEntry(0, 30, first);
            _effect.Tick(10, 0.1f);

            _effects[EffectType.Burning] = new EffectEntry(0, 40, next);
            _effect.Tick(20, 0.1f);

            _damageService.Received(1).Damage(6, _owner.Entity, first,
                HealthEventId.Create(_owner.GetType(), 10), includeStats: false);
            _damageService.Received(1).Damage(6, _owner.Entity, next,
                HealthEventId.Create(_owner.GetType(), 20), includeStats: false);
        }

        [Test]
        public void Tick_RemovedEffect_StopsDamage()
        {
            _effects[EffectType.Burning] = new EffectEntry(0, 30, null);
            _effect.Tick(10, 0.1f);
            _damageService.ClearReceivedCalls();

            _effects.Clear();
            _effect.Tick(20, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public void Tick_ExpiredEffect_DoesNotDamage()
        {
            EffectOwnerBehaviour effects = new GameObject().AddComponent<EffectOwnerBehaviour>();
            effects.AddEffect(EffectType.Burning, 1);
            _owner.Effects.Returns(effects.Effects);

            try
            {
                effects.OnTick(10, 0.1f);
                _effect.Tick(10, 0.1f);

                Assert.That(_damageService.ReceivedCalls(), Is.Empty);
            }
            finally { Object.DestroyImmediate(effects.gameObject); }
        }

        [Test]
        public void OverlappingIds_DamageOnceAndCreditLatestActiveSource()
        {
            EffectOwnerBehaviour effects = new GameObject().AddComponent<EffectOwnerBehaviour>();
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour latest = Substitute.For<IEntityBehaviour>();
            _owner.Effects.Returns(effects.Effects);

            try
            {
                effects.AddEffect(EffectType.Burning, 10, first);
                effects.AddEffect(EffectType.Burning, 2, latest);
                effects.OnTick(10, 0.1f);
                _effect.Tick(10, 0.1f);

                _damageService.Received(1).Damage(6, _owner.Entity, latest,
                    HealthEventId.Create(_owner.GetType(), 10), includeStats: false);
                _damageService.DidNotReceive().Damage(Arg.Any<uint>(), _owner.Entity, first,
                    Arg.Any<HealthEventId>(), includeStats: false);

                _damageService.ClearReceivedCalls();
                effects.OnTick(20, 0.1f);
                _effect.Tick(20, 0.1f);

                _damageService.Received(1).Damage(6, _owner.Entity, first,
                    HealthEventId.Create(_owner.GetType(), 20), includeStats: false);
            }
            finally { Object.DestroyImmediate(effects.gameObject); }
        }
    }
}
