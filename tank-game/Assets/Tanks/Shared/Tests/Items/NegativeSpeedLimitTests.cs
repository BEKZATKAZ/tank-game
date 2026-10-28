using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Tests.Items
{
    public class NegativeSpeedLimitTests : ItemBehaviourTestBase<NegativeSpeedLimitBehaviour>
    {
        protected override void InitializeConfig()
        {
            AddItemStat("decrease", new ItemStat { Value = 90 });
        }

        [TestCase(100, 10)]
        [TestCase(8, 0.8f)]
        [TestCase(0, 0)]
        public void DealDamage_SelfDamage_ReducesByConfiguredPercentage(float damage, float expected)
        {
            Behaviour.OnDealDamage(MockOwner.Entity, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void DealDamage_OtherTarget_DoesNotChangeDamage()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            float damage = 100;

            Behaviour.OnDealDamage(target, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(100));
        }
    }
}
