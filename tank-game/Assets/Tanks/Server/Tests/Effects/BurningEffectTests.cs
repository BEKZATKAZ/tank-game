using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Effects;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

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
        public void Tick_BurningOnDamageTick_DealsTenDamageWithoutStats(int tick)
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            _effects[EffectType.Burning] = new EffectEntry(0, 30, from);

            _effect.Tick(tick, 0.1f);

            _damageService.Received(1).Damage(10, _owner.Entity, from,
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

            _damageService.Received(1).Damage(10, _owner.Entity, first,
                HealthEventId.Create(_owner.GetType(), 10), includeStats: false);
            _damageService.Received(1).Damage(10, _owner.Entity, next,
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
    }
}
