using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Tests.Items
{
    public class ChickenEggTests : ItemBehaviourTestBase<ChickenEggBehaviour>
    {
        protected override void InitializeConfig()
        {
            AddItemStat("threshold", new ItemStat { Value = 90 });
            AddItemStat("damage", new ItemStat { Value = 15 });
        }

        [TestCase(100, 100u, 115)]
        [TestCase(89, 100u, 100)]
        [TestCase(179, 200u, 100)]
        [TestCase(0, 100u, 100)]
        public void DealDamage_UsesTargetHealthPercentage(int current, uint max, float expected)
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();

            target
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(current, max));

            MockOwner
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(1, 100));
            float damage = 100;

            Behaviour.OnDealDamage(target, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void DealDamage_ReevaluatesTargetHealthOnEachHit()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();

            target.
                GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(100, 100));

            float damage = 40;

            Behaviour.OnDealDamage(target, MockOwner, ref damage);
            Assert.That(damage, Is.EqualTo(46).Within(0.001f));

            target
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(50, 100));

            damage = 40;
            Behaviour.OnDealDamage(target, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(40));
        }
    }
}
