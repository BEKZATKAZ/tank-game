using NSubstitute;
using NUnit.Framework;
using System.Reflection;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using UnityEngine;

namespace Tanks.Shared.Tests.Effects
{
    public class EffectOwnerBehaviourTests
    {
        private EffectOwnerBehaviour _owner;

        [SetUp]
        public void Setup()
        {
            _owner = new GameObject().AddComponent<EffectOwnerBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_owner) Object.DestroyImmediate(_owner.gameObject);
        }

        [TestCase(EffectType.Burning)]
        [TestCase(EffectType.Frozen)]
        public void AddEffect_RecordsSimulationTickDurationAndSource(EffectType type)
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            _owner.OnTick(100, 0.25f);

            _owner.AddEffect(type, 5, from);

            EffectEntry effect = _owner.Effects[type];
            Assert.That(effect.StartTick, Is.EqualTo(100));
            Assert.That(effect.Duration, Is.EqualTo(5));
            Assert.That(effect.From, Is.SameAs(from));
        }

        [Test]
        public void AddEffect_StacksDurationAndUsesLatestSource()
        {
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour second = Substitute.For<IEntityBehaviour>();
            _owner.OnTick(100, 0.25f);
            _owner.AddEffect(EffectType.Burning, 5, first);
            _owner.OnTick(108, 0.25f);

            _owner.AddEffect(EffectType.Burning, 3, second);

            EffectEntry effect = _owner.Effects[EffectType.Burning];
            Assert.That(effect.StartTick, Is.EqualTo(100));
            Assert.That(effect.Duration, Is.EqualTo(8));
            Assert.That(effect.From, Is.SameAs(second));

            _owner.OnTick(131, 0.25f);
            Assert.That(_owner.Effects.ContainsKey(EffectType.Burning), Is.True);

            _owner.OnTick(132, 0.25f);
            Assert.That(_owner.Effects, Is.Empty);
        }

        [Test]
        public void AddEffect_WithoutSource_ClearsPreviousSource()
        {
            _owner.AddEffect(EffectType.Burning, 5, Substitute.For<IEntityBehaviour>());

            _owner.AddEffect(EffectType.Burning, 2);

            Assert.That(_owner.Effects[EffectType.Burning].From, Is.Null);
        }

        [TestCase(EffectType.Burning, EffectType.Frozen)]
        [TestCase(EffectType.Frozen, EffectType.Burning)]
        public void AddEffect_OppositeEffect_ReplacesAndRestartsDuration(EffectType first, EffectType next)
        {
            _owner.AddEffect(first, 30);
            _owner.OnTick(10, 0.25f);

            _owner.AddEffect(next, 2);

            Assert.That(_owner.Effects.Count, Is.EqualTo(1));
            Assert.That(_owner.Effects.ContainsKey(first), Is.False);
            Assert.That(_owner.Effects[next].StartTick, Is.EqualTo(10));
            Assert.That(_owner.Effects[next].Duration, Is.EqualTo(2));
        }

        [TestCase(EffectType.Burning)]
        [TestCase(EffectType.Frozen)]
        public void Tick_ExpiresExactlyAtDurationAfterSkippedTicks(EffectType type)
        {
            _owner.OnTick(1000, 0.25f);
            _owner.AddEffect(type, 2);

            _owner.OnTick(1007, 0.25f);
            Assert.That(_owner.Effects.ContainsKey(type), Is.True);

            _owner.OnTick(1008, 0.25f);
            Assert.That(_owner.Effects, Is.Empty);
        }

        [Test]
        public void Tick_ExpiredEffect_CanBeAppliedAgain()
        {
            _owner.AddEffect(EffectType.Burning, 1);
            _owner.OnTick(100, 0.25f);

            _owner.AddEffect(EffectType.Burning, 2);

            Assert.That(_owner.Effects[EffectType.Burning].StartTick, Is.EqualTo(100));
            Assert.That(_owner.Effects[EffectType.Burning].Duration, Is.EqualTo(2));
        }

        [TestCase(EffectType.Burning, EffectType.Frozen)]
        [TestCase(EffectType.Frozen, EffectType.Burning)]
        public void OverrideEffect_ReplicatedEntry_ReplacesOppositeWithoutStacking(EffectType first, EffectType next)
        {
            _owner.AddEffect(first, 30);
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();
            EffectEntry entry = new EffectEntry(100, 4, from);
            MethodInfo apply = typeof(EffectOwnerBehaviour).GetMethod(
                "OverrideEffect", BindingFlags.Instance | BindingFlags.NonPublic);

            apply.Invoke(_owner, new object[] { next, entry });
            apply.Invoke(_owner, new object[] { next, entry });

            Assert.That(_owner.Effects.Count, Is.EqualTo(1));
            Assert.That(_owner.Effects.ContainsKey(first), Is.False);
            Assert.That(_owner.Effects[next].StartTick, Is.EqualTo(100));
            Assert.That(_owner.Effects[next].Duration, Is.EqualTo(4));
            Assert.That(_owner.Effects[next].From, Is.SameAs(from));

            _owner.OnTick(116, 0.25f);
            Assert.That(_owner.Effects, Is.Empty);
        }
    }
}
