using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Tests.Items
{
    public class SelfConsciousTests : ItemBehaviourTestBase<SelfConsciousBehaviour>
    {
        protected override void InitializeConfig()
        {
            AddItemStat("damage", new ItemStat { Value = 150 });
        }

        [TestCase(0, 0)]
        [TestCase(20, 50)]
        [TestCase(1, 2.5f)]
        public void DealDamage_SelfDamage_IncreasesByConfiguredPercentage(float damage, float expected)
        {
            Behaviour.OnDealDamage(MockOwner.Entity, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void DealDamage_OtherTarget_DoesNotChangeDamage()
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            float damage = 20;

            Behaviour.OnDealDamage(target, MockOwner, ref damage);

            Assert.That(damage, Is.EqualTo(20));
        }
    }
}
