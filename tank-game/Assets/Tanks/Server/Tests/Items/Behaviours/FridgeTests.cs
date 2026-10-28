using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Server.Items.Behaviours.HealthBased;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using Tanks.Shared.Weapons;
using EntityId = Tanks.Shared.Entities.EntityId;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class FridgeTests : ItemBehaviourTestBase<FridgeBehaviour>
    {
        private IStatOwnerBehaviour _stats;
        private IReadOnlyWeapon _weapon;

        protected override void InitializeConfig()
        {
            AddItemStat("increase", new ItemStat { Value = 15, ValueType = StatValueType.Percent });
            AddItemStat("decrease", new ItemStat { Value = -3, ValueType = StatValueType.Flat });
        }

        public override void Setup()
        {
            base.Setup();

            _stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            _stats.GetStat(Arg.Any<StatType>(), Arg.Any<float>()).Returns(args => (float)args[1]);

            _weapon = Substitute.For<IReadOnlyWeapon>();
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);
            MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns(_weapon);
            MockOwner.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed.Returns(10);
        }

        [Test]
        public void HealthChanged_Healing_IncreasesLowestAndDecreasesHighestStat()
        {
            Behaviour.OnPickup(MockOwner);

            Heal();

            AssertChanges(_stats, StatType.Damage, StatType.Speed);
        }

        [Test]
        public void HealthChanged_OneHealthPoint_AppliesFullStatChanges()
        {
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(50, 100), new HealthState(51, 100));

            AssertChanges(_stats, StatType.Damage, StatType.Speed);
        }

        [Test]
        public void HealthChanged_OnlyMaximumHealthIncreases_DoesNothing()
        {
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(50, 100), new HealthState(50, 200));

            AssertNoChanges();
        }

        [TestCase(0)]
        [TestCase(25)]
        [TestCase(50)]
        public void HealthChanged_DamageOrUnchangedHealth_DoesNothing(int health)
        {
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(50, 100), new HealthState(health, 100));

            AssertNoChanges();
        }

        [Test]
        public void HealthChanged_BeforePickup_DoesNothing()
        {
            Heal();

            AssertNoChanges();
        }

        [Test]
        public void HealthChanged_UsesModifiedStatsToChooseLowestAndHighest()
        {
            _stats.GetStat(StatType.Resistance, 0).Returns(-5);
            _stats.GetStat(StatType.Defense, 0).Returns(30);
            Behaviour.OnPickup(MockOwner);

            Heal();

            AssertChanges(_stats, StatType.Resistance, StatType.Defense);
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(-5)]
        public void HealthChanged_AllStatsEqual_DoesNothing(float value)
        {
            _stats.GetStat(Arg.Any<StatType>(), Arg.Any<float>()).Returns(value);
            Behaviour.OnPickup(MockOwner);

            Heal();

            AssertNoChanges();
        }

        [Test]
        public void HealthChanged_TiedExtremes_UsesFirstStatOfEachValue()
        {
            _stats.GetStat(Arg.Any<StatType>(), Arg.Any<float>()).Returns(10);
            _stats.GetStat(StatType.Speed, Arg.Any<float>()).Returns(-5);
            _stats.GetStat(StatType.Defense, Arg.Any<float>()).Returns(-5);
            Behaviour.OnPickup(MockOwner);

            Heal();

            AssertChanges(_stats, StatType.Speed, StatType.Damage);
        }

        [Test]
        public void HealthChanged_EachHeal_ReevaluatesStats()
        {
            Behaviour.OnPickup(MockOwner);
            Heal();
            AssertChanges(_stats, StatType.Damage, StatType.Speed);
            _stats.ClearReceivedCalls();

            _stats.GetStat(StatType.Defense, 0).Returns(-5);
            _stats.GetStat(StatType.Resistance, 0).Returns(30);
            Heal();

            AssertChanges(_stats, StatType.Defense, StatType.Resistance);
        }

        [TestCase(0.25f, 4)]
        [TestCase(0, 0)]
        [TestCase(-1, 0)]
        [TestCase(0.0000001f, 0)]
        public void HealthChanged_UsesCurrentWeaponCooldown(float cooldown, float rateOfFire)
        {
            Behaviour.OnPickup(MockOwner);
            IReadOnlyWeapon other = Substitute.For<IReadOnlyWeapon>();
            other.Cooldown.DurationSeconds.Returns(cooldown);
            MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns(other);

            Heal();

            _stats.Received(1).GetStat(StatType.RateOfFire, rateOfFire);
            _stats.Received(1).GetStat(StatType.Speed, 10);
            _stats.Received(1).GetStat(StatType.Damage, 0);
            _stats.Received(1).GetStat(StatType.Defense, 0);
            _stats.Received(1).GetStat(StatType.Resistance, 0);
        }

        [TestCase("stats")]
        [TestCase("holder")]
        [TestCase("weapon")]
        [TestCase("cooldown")]
        [TestCase("drive")]
        public void HealthChanged_MissingDependency_DoesNothing(string dependency)
        {
            switch (dependency)
            {
                case "stats": MockOwner.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null); break;
                case "holder": MockOwner.GetBehaviour<IWeaponHolderBehaviour>().Returns((IWeaponHolderBehaviour)null); break;
                case "weapon": MockOwner.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns((IReadOnlyWeapon)null); break;
                case "cooldown": _weapon.Cooldown.Returns((IReadOnlyCooldown)null); break;
                case "drive": MockOwner.GetBehaviour<ITankDriveBehaviour>().Returns((ITankDriveBehaviour)null); break;
            }

            Behaviour.OnPickup(MockOwner);
            Heal();

            AssertNoChanges();
        }

        [Test]
        public void Pickup_Twice_PreservesEntryAndAppliesOnlyOncePerHeal()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.ClearReceivedCalls();

            Behaviour.OnPickup(MockOwner);

            MockOwner.DidNotReceive().GetBehaviour<ITankDriveBehaviour>();
            MockOwner.DidNotReceive().GetBehaviour<IWeaponHolderBehaviour>();
            AssertNoChanges();

            Heal();

            AssertChanges(_stats, StatType.Damage, StatType.Speed);
        }

        [Test]
        public void Destroy_StopsChangesUntilPickedUpAgain()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnDestroy(MockOwner);

            Heal();
            AssertNoChanges();

            Behaviour.OnPickup(MockOwner);
            Heal();

            AssertChanges(_stats, StatType.Damage, StatType.Speed);
        }

        [Test]
        public void Destroy_WithoutPickup_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Behaviour.OnDestroy(MockOwner));
        }

        [Test]
        public void HealthChanged_OwnersAreIndependent()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            other.Entity.Id.Returns(new EntityId(2));
            other.GetBehaviour<IWeaponHolderBehaviour>().CurrentWeapon.Returns(_weapon);
            other.GetBehaviour<ITankDriveBehaviour>().DefaultSpeed.Returns(20);

            IStatOwnerBehaviour otherStats = other.GetBehaviour<IStatOwnerBehaviour>();
            otherStats.GetStat(Arg.Any<StatType>(), Arg.Any<float>()).Returns(args => (float)args[1]);
            otherStats.GetStat(StatType.Resistance, 0).Returns(-5);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(other);

            Heal();
            AssertChanges(_stats, StatType.Damage, StatType.Speed);
            otherStats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);

            Behaviour.OnDestroy(MockOwner);
            Behaviour.OnHealthChanged(other, null, new HealthState(50, 100), new HealthState(60, 100));

            AssertChanges(otherStats, StatType.Resistance, StatType.Speed);
        }

        private void Heal()
        {
            Behaviour.OnHealthChanged(MockOwner, null, new HealthState(50, 100), new HealthState(60, 100));
        }

        private void AssertChanges(IStatOwnerBehaviour stats, StatType lowest, StatType highest)
        {
            stats.Received(1).AddStat(lowest, StatValueType.Percent, 15);
            stats.Received(1).AddStat(highest, StatValueType.Flat, -3);
            Assert.That(stats.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "AddStat"), Is.EqualTo(2));
        }

        private void AssertNoChanges()
        {
            _stats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);
        }
    }
}
