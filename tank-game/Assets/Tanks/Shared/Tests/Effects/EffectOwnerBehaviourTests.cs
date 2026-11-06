using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using UnityEngine;
using EntityId = Tanks.Shared.Entities.EntityId;

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
        public void ExtendEffect_StacksOnlyItsDurationAndUsesSimulationTime()
        {
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            _owner.OnTick(100, 0.25f);
            EffectId id = _owner.AddEffect(EffectType.Burning, 5, first);
            _owner.OnTick(108, 0.25f);

            _owner.ExtendEffect(id, 3);

            EffectEntry effect = _owner.Effects[EffectType.Burning];
            Assert.That(effect.StartTick, Is.EqualTo(100));
            Assert.That(effect.Duration, Is.EqualTo(8));
            Assert.That(effect.From, Is.SameAs(first));

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
        [TestCase(EffectType.Flashed)]
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
            EffectEntry entry = new EffectEntry(next, 100, 4, from);
            EffectId id = EffectIdGenerator.GenerateNext();
            MethodInfo apply = typeof(EffectOwnerBehaviour).GetMethod(
                "SetEffect", BindingFlags.Instance | BindingFlags.NonPublic);

            apply.Invoke(_owner, new object[] { id, entry });
            apply.Invoke(_owner, new object[] { id, entry });

            Assert.That(_owner.Effects.Count, Is.EqualTo(1));
            Assert.That(_owner.Effects.ContainsKey(first), Is.False);
            Assert.That(_owner.Effects[next].StartTick, Is.EqualTo(100));
            Assert.That(_owner.Effects[next].Duration, Is.EqualTo(4));
            Assert.That(_owner.Effects[next].From, Is.SameAs(from));

            _owner.OnTick(116, 0.25f);
            Assert.That(_owner.Effects, Is.Empty);
        }

        [Test]
        public void AddEffect_CreatesIndependentIdsAndDurations()
        {
            EffectId first = _owner.AddEffect(EffectType.Burning, 10);
            EffectId second = _owner.AddEffect(EffectType.Burning, 3);
            _owner.OnTick(2, 1);

            _owner.OverrideEffect(second, EffectType.Burning, 2);

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(_owner.Entries.Count, Is.EqualTo(2));
            Assert.That(_owner.TryGetEffect(first, out EffectEntry entry), Is.True);
            Assert.That(entry.StartTick, Is.Zero);
            Assert.That(entry.Duration, Is.EqualTo(10));

            _owner.OnTick(4, 1);

            Assert.That(_owner.HasEffect(first), Is.True);
            Assert.That(_owner.HasEffect(second), Is.False);
            Assert.That(_owner.Effects[EffectType.Burning].Duration, Is.EqualTo(10));
        }

        [Test]
        public void ApplyEffect_KeysRegistryByAttackerProducerAndType()
        {
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour second = Substitute.For<IEntityBehaviour>();
            first.Id.Returns(new EntityId(1));
            second.Id.Returns(new EntityId(2));

            EffectId weapon = _owner.ApplyEffect(EffectType.Burning, 5, "weapon", first, restart: true);
            EffectId item = _owner.ApplyEffect(EffectType.Burning, 10, "item", first);
            EffectId attacker = _owner.ApplyEffect(EffectType.Burning, 8, "weapon", second, restart: true);
            EffectId healing = _owner.ApplyEffect(EffectType.Healing, 5, "weapon", first);
            _owner.OnTick(2, 1);

            EffectId refreshed = _owner.ApplyEffect(EffectType.Burning, 5, "weapon", first, restart: true);

            Assert.That(refreshed, Is.EqualTo(weapon));
            Assert.That(_owner.Entries.Count, Is.EqualTo(4));
            Assert.That(_owner.Entries[weapon].StartTick, Is.EqualTo(2));
            Assert.That(_owner.Entries[item].Duration, Is.EqualTo(10));
            Assert.That(_owner.Entries[attacker].StartTick, Is.Zero);
            Assert.That(_owner.Entries[healing].Type, Is.EqualTo(EffectType.Healing));
        }

        [Test]
        public void ApplyEffect_ExtendsItsProducerAndEvictsExpiredRegistryEntries()
        {
            EffectId id = _owner.ApplyEffect(EffectType.Healing, 5, "flower");
            _owner.OnTick(2, 1);

            Assert.That(_owner.ApplyEffect(EffectType.Healing, 3, "flower"), Is.EqualTo(id));
            Assert.That(_owner.Entries[id].Duration, Is.EqualTo(8));
            Assert.That(_owner.Entries[id].StartTick, Is.Zero);

            _owner.OnTick(8, 1);

            EffectId next = _owner.ApplyEffect(EffectType.Healing, 5, "flower");
            Assert.That(next, Is.Not.EqualTo(id));
            Assert.That(_owner.Entries[next].StartTick, Is.EqualTo(8));
        }

        [TestCase(EffectType.Burning, EffectType.Frozen)]
        [TestCase(EffectType.Frozen, EffectType.Burning)]
        public void OppositeEffect_RemovesEveryIdAndReportsEachReplacement(EffectType first, EffectType next)
        {
            EffectId one = _owner.ApplyEffect(first, 5, "one");
            EffectId two = _owner.ApplyEffect(first, 8, "two");
            List<EffectId> ended = new();
            List<EffectEndReason> reasons = new();
            int typeEnds = 0;
            _owner.EntryEnded += (id, type, reason) =>
            {
                ended.Add(id);
                reasons.Add(reason);
            };
            _owner.Ended += (type, reason) => typeEnds++;

            _owner.AddEffect(next, 10);

            Assert.That(ended, Is.EquivalentTo(new[] { one, two }));
            Assert.That(reasons, Is.EqualTo(new[] { EffectEndReason.Replaced, EffectEndReason.Replaced }));
            Assert.That(typeEnds, Is.EqualTo(1));
            Assert.That(_owner.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveEffect_ReportsEntryAndOnlyEndsTypeAfterFinalId()
        {
            EffectId first = _owner.AddEffect(EffectType.Healing, 3);
            EffectId second = _owner.AddEffect(EffectType.Healing, 8);
            List<EffectEndReason> entries = new();
            List<EffectEndReason> types = new();
            _owner.EntryEnded += (id, type, reason) => entries.Add(reason);
            _owner.Ended += (type, reason) => types.Add(reason);

            _owner.RemoveEffect(first);
            _owner.RemoveEffect(first);

            Assert.That(entries, Is.EqualTo(new[] { EffectEndReason.Removed }));
            Assert.That(types, Is.Empty);

            _owner.OnTick(8, 1);

            Assert.That(entries, Is.EqualTo(new[] { EffectEndReason.Removed, EffectEndReason.Expired }));
            Assert.That(types, Is.EqualTo(new[] { EffectEndReason.Expired }));
            Assert.That(_owner.HasEffect(second), Is.False);
        }

        [Test]
        public void Burning_UsesLatestUpdatedActiveSourceAndFallsBackAfterExpiry()
        {
            IEntityBehaviour first = Substitute.For<IEntityBehaviour>();
            IEntityBehaviour second = Substitute.For<IEntityBehaviour>();
            EffectId longBurn = _owner.AddEffect(EffectType.Burning, 10, first);
            _owner.AddEffect(EffectType.Burning, 3, second);

            Assert.That(_owner.Effects[EffectType.Burning].From, Is.SameAs(second));
            Assert.That(_owner.Effects[EffectType.Burning].Duration, Is.EqualTo(10));

            _owner.ExtendEffect(longBurn, 1);
            Assert.That(_owner.Effects[EffectType.Burning].From, Is.SameAs(first));

            _owner.AddEffect(EffectType.Burning, 1, second);
            _owner.OnTick(3, 1);

            Assert.That(_owner.Effects[EffectType.Burning].From, Is.SameAs(first));
        }
    }
}
