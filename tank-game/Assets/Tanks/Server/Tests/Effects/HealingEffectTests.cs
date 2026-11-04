using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Effects;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Tests.Effects
{
    public class HealingEffectTests
    {
        private HealingEffect _effect;
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

            _effect = new HealingEffect(_damageService);
            _effect.Initialize(_owner);
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(10)]
        public void Tick_HealingOnHealTick_HealsOwnerByFive(int tick)
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 30, null);

            _effect.Tick(tick, 0.1f);

            _damageService.Received(1).Heal(5, _owner.Entity);
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default, default, default, default);
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(9)]
        public void Tick_BetweenHealTicks_DoesNotHeal(int tick)
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 30, null);

            _effect.Tick(tick, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public void Tick_NoEffects_DoesNotHeal()
        {
            _effect.Tick(5, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [TestCase(EffectType.Burning)]
        [TestCase(EffectType.Frozen)]
        public void Tick_UnrelatedEffect_DoesNotHeal(EffectType type)
        {
            _effects[type] = new EffectEntry(0, 30, null);

            _effect.Tick(5, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }

        [Test]
        public void Tick_ConsecutiveHealTicks_HealsEveryFiveTicks()
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 30, null);

            for (int tick = 1; tick <= 60; tick++) _effect.Tick(tick, 0.1f);

            _damageService.Received(12).Heal(5, _owner.Entity);
        }

        [Test]
        public void Tick_RemovedEffect_StopsHealing()
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 30, null);
            _effect.Tick(5, 0.1f);
            _damageService.Received(1).Heal(5, _owner.Entity);
            _damageService.ClearReceivedCalls();

            _effects.Clear();
            _effect.Tick(10, 0.1f);

            Assert.That(_damageService.ReceivedCalls(), Is.Empty);
        }
    }
}
