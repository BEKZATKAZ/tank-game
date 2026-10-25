using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.HealthBased;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using UnityEngine;
using EntityId = Tanks.Shared.Entities.EntityId;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class BurstOfAdrenalineTests : ItemBehaviourTestBase<BurstOfAdrenalineBehaviour>
    {
        protected override void InitializeConfig()
        {
            AddItemStat("health-threshold", new ItemStat { Value = 20 });
            AddItemStat("duration", new ItemStat { Value = 7.5f });
            AddItemStat("speed-boost", new ItemStat { Value = 50, ValueType = StatValueType.Percent });
            AddItemStat("rate-of-fire-boost", new ItemStat { Value = 50, ValueType = StatValueType.Percent });
        }

        [TestCase(1)]
        [TestCase(19)]
        public void HealthChanged_BelowThreshold_AppliesTemporaryBoosts(int health)
        {
            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(100, 100), new HealthState(health, 100));

            AssertBoosts(MockOwner);
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(20)]
        [TestCase(100)]
        public void HealthChanged_DeadOrAboveThreshold_DoesNothing(int health)
        {
            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(health, 100));

            MockOwner.GetBehaviour<IStatOwnerBehaviour>().DidNotReceiveWithAnyArgs()
                .AddStat(default, default, default, default);
        }

        [Test]
        public void Pickup_AlreadyLowHealth_ActivatesImmediately()
        {
            MockOwner.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(19, 100));

            Behaviour.OnPickup(MockOwner);

            AssertBoosts(MockOwner);
        }

        [Test]
        public void HealthChanged_RepeatedDamageHealingAndPickup_ActivatesOnlyOnce()
        {
            MockOwner.GetBehaviour<IHealthBehaviour>().ServerState.Value.Returns(new HealthState(19, 100));
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(19, 100), new HealthState(10, 100));
            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(10, 100), new HealthState(100, 100));
            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(100, 100), new HealthState(19, 100));
            Behaviour.OnPickup(MockOwner);

            AssertBoosts(MockOwner);
        }

        [Test]
        public void HealthChanged_OwnersActivateIndependently()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            other.Entity.Id.Returns(new EntityId(2));

            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(19, 100));
            Behaviour.OnHealthChanged(other, null, default, new HealthState(19, 100));

            AssertBoosts(MockOwner);
            AssertBoosts(other);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NextRound_CanActivateAgain(bool reset)
        {
            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(19, 100));
            if (reset) Behaviour.Reset();
            else Behaviour.OnDestroy(MockOwner);
            MockOwner.GetBehaviour<IStatOwnerBehaviour>().ClearReceivedCalls();

            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(19, 100));

            AssertBoosts(MockOwner);
        }

        [Test]
        public void Boosts_RealStats_ExpireAndDoNotReactivate()
        {
            GameObject target = new GameObject();
            StatTypeParametersConfig parameters = ScriptableObject.CreateInstance<StatTypeParametersConfig>();

            try
            {
                StatOwnerBehaviour stats = target.AddComponent<StatOwnerBehaviour>();
                stats.Config = parameters;
                MockOwner.GetBehaviour<IStatOwnerBehaviour>().Returns(stats);

                Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(19, 100));

                Assert.That(stats.GetStat(StatType.Speed, 10), Is.EqualTo(15));
                Assert.That(stats.GetStat(StatType.RateOfFire, 2), Is.EqualTo(3));
                stats.OnTick(14, 0.5f);
                Assert.That(stats.GetStat(StatType.Speed, 10), Is.EqualTo(15));
                stats.OnTick(15, 0.5f);
                Assert.That(stats.GetStat(StatType.Speed, 10), Is.EqualTo(10));
                Assert.That(stats.GetStat(StatType.RateOfFire, 2), Is.EqualTo(2));

                Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(10, 100));
                Assert.That(stats.GetStat(StatType.Speed, 10), Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(parameters);
            }
        }

        private void AssertBoosts(IItemOwnerBehaviour owner)
        {
            IStatOwnerBehaviour stats = owner.GetBehaviour<IStatOwnerBehaviour>();
            stats.Received(1).AddStat(StatType.Speed, StatValueType.Percent, 50, 7.5f);
            stats.Received(1).AddStat(StatType.RateOfFire, StatValueType.Percent, 50, 7.5f);
        }
    }
}
