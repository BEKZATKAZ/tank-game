using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Driving;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using Tanks.Shared.Weapons;
using UnityEditor;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class ReRerollTests : ItemBehaviourTestBase<ReRerollBehaviour>
    {
        private IStatOwnerBehaviour _stats;
        private IReadOnlyWeapon _weapon;
        private Dictionary<StatId, (StatType Type, float Value)> _applied;
        private UnityEngine.Random.State _randomState;

        protected override void InitializeConfig()
        {
            AddItemStat("interval", new ItemStat { Value = 10 });
        }

        public override void Setup()
        {
            base.Setup();
            _randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(42);
            _applied = new Dictionary<StatId, (StatType Type, float Value)>();
            _stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            _weapon = Substitute.For<IReadOnlyWeapon>();
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);
            MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns(_weapon);
            MockOwner.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed.Returns(10);

            _stats.GetStat(Arg.Any<StatType>(), Arg.Any<float>()).Returns(args =>
            {
                StatType type = (StatType)args[0];
                return (float)args[1] + _applied.Values.Where(x => x.Type == type).Sum(x => x.Value);
            });
            _stats.When(x => x.OverrideStat(Arg.Any<StatId>(), Arg.Any<StatType>(),
                Arg.Any<StatValueType>(), Arg.Any<float>(), Arg.Any<float>()))
                .Do(args => _applied[(StatId)args[0]] = ((StatType)args[1], (float)args[3]));
            _stats.When(x => x.RemoveStat(Arg.Any<StatId>())).Do(args => _applied.Remove((StatId)args[0]));
        }

        [TearDown]
        public void TearDown() => UnityEngine.Random.state = _randomState;

        [TestCase(0.001f)]
        [TestCase(0.02f)]
        public void Tick_FirstTick_ShufflesValuesWithoutAddingBaseStats(float tickDelta)
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, tickDelta);

            Assert.That(CurrentValues(), Is.EquivalentTo(new float[] { 0, 10, 0, 0, 2 }));
            Assert.That(_applied.Count, Is.EqualTo(2));
            Assert.That(_applied.Values.Select(stat => stat.Type),
                Is.EquivalentTo(new[] { StatType.Speed, StatType.RateOfFire }));
            Assert.That(CurrentValues()[0], Is.Zero);
            Assert.That(CurrentValues()[2], Is.Zero);
            Assert.That(CurrentValues()[3], Is.Zero);
        }

        [Test]
        public void Tick_RepeatedRerolls_DoNotAccumulateAndPreserveOtherBonuses()
        {
            StatId bonusId = StatIdGenerator.GenerateNext();
            _stats.OverrideStat(bonusId, StatType.Defense, StatValueType.Flat, 7);
            Behaviour.OnPickup(MockOwner);

            for (int tick = 0; tick <= 100; tick += 10)
            {
                Behaviour.OnTick(MockOwner, tick, 1);
                Assert.That(CurrentValues(), Is.EquivalentTo(new float[] { 0, 10, 7, 0, 2 }));
                Assert.That(_applied.Count, Is.EqualTo(4));
                Assert.That(_applied.ContainsKey(bonusId), Is.True);
                Assert.That(CurrentValues()[0], Is.Zero);
                Assert.That(CurrentValues()[3], Is.Zero);
            }
        }

        [Test]
        public void Tick_BeforeInterval_DoesNotReroll()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 0.5f);
            _stats.ClearReceivedCalls();

            Behaviour.OnTick(MockOwner, 19, 0.5f);
            _stats.DidNotReceiveWithAnyArgs().OverrideStat(default, default, default, default, default);
            Behaviour.OnTick(MockOwner, 20, 0.5f);
            Assert.That(_stats.ReceivedCalls().Count(x => x.GetMethodInfo().Name == "OverrideStat"), Is.EqualTo(2));
        }

        [Test]
        public void Pickup_Twice_DoesNotRerollBeforeInterval()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 1);
            Behaviour.OnPickup(MockOwner);
            _stats.ClearReceivedCalls();

            Behaviour.OnTick(MockOwner, 1, 1);

            _stats.DidNotReceiveWithAnyArgs().OverrideStat(default, default, default, default, default);
        }

        [Test]
        public void Destroy_RemovesOnlyOwnModifiersAndStopsTicking()
        {
            StatId bonusId = StatIdGenerator.GenerateNext();
            _stats.OverrideStat(bonusId, StatType.Defense, StatValueType.Flat, 7);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 1);

            Behaviour.OnDestroy(MockOwner);
            Assert.DoesNotThrow(() => Behaviour.OnTick(MockOwner, 10, 1));

            Assert.That(_applied.Keys, Is.EquivalentTo(new[] { bonusId }));
            Assert.That(CurrentValues(), Is.EqualTo(new float[] { 0, 10, 7, 0, 2 }));
        }

        [TestCase(0)]
        [TestCase(0.25f)]
        public void Tick_UsesCurrentWeaponCooldown(float cooldown)
        {
            Behaviour.OnPickup(MockOwner);
            _weapon.Cooldown.DurationSeconds.Returns(cooldown);

            Behaviour.OnTick(MockOwner, 0, 1);

            Assert.That(CurrentValues(), Is.EquivalentTo(new float[] { 0, 10, 0, 0, cooldown > 0 ? 1 / cooldown : 0 }));
        }

        [Test]
        public void Tick_NoWeapon_DoesNotThrowAndRetriesWhenEquipped()
        {
            MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns((IReadOnlyWeapon)null);
            Behaviour.OnPickup(MockOwner);
            Assert.DoesNotThrow(() => Behaviour.OnTick(MockOwner, 0, 1));
            Assert.That(_applied, Is.Empty);
            MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns(_weapon);

            Behaviour.OnTick(MockOwner, 1, 1);

            Assert.That(_applied.Count, Is.EqualTo(2));
        }

        [Test]
        public void Tick_AllStatsZero_DoesNotApplyModifiers()
        {
            MockOwner.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed.Returns(0);
            _weapon.Cooldown.DurationSeconds.Returns(0);
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnTick(MockOwner, 0, 1);

            Assert.That(_applied, Is.Empty);
            Assert.That(CurrentValues(), Is.All.Zero);
        }

        [Test]
        public void Tick_OnlyOneNonzeroStat_KeepsItsValueAndType()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0);
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnTick(MockOwner, 0, 1);

            Assert.That(CurrentValues(), Is.EqualTo(new float[] { 0, 10, 0, 0, 0 }));
            Assert.That(_applied.Values.Select(stat => stat.Type), Is.EquivalentTo(new[] { StatType.Speed }));
        }

        [Test]
        public void Tick_BonusRemoved_ExcludesStatAndRemovesPreviousRerollModifier()
        {
            StatId bonusId = StatIdGenerator.GenerateNext();
            _stats.OverrideStat(bonusId, StatType.Defense, StatValueType.Flat, -7);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 1);
            Assert.That(CurrentValues(), Is.EquivalentTo(new float[] { 0, 10, -7, 0, 2 }));

            _stats.RemoveStat(bonusId);
            Behaviour.OnTick(MockOwner, 10, 1);

            Assert.That(CurrentValues(), Is.EquivalentTo(new float[] { 0, 10, 0, 0, 2 }));
            Assert.That(CurrentValues()[2], Is.Zero);
            Assert.That(_applied.Values.Any(stat => stat.Type == StatType.Defense), Is.False);
        }

        [Test]
        public void Tick_BeforePickup_DoesNothing()
        {
            Assert.DoesNotThrow(() => Behaviour.OnTick(MockOwner, 0, 1));
            Assert.That(_applied, Is.Empty);
        }

        private float[] CurrentValues()
        {
            return ((StatType[])Enum.GetValues(typeof(StatType)))
                .Select(type => _stats.GetStat(type, type switch
                {
                    StatType.Speed => MockOwner.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed,
                    StatType.RateOfFire => _weapon.Cooldown.DurationSeconds > 0 ? 1 / _weapon.Cooldown.DurationSeconds : 0,
                    _ => 0
                }))
                .ToArray();
        }
    }
}
