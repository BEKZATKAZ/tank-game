using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class HealthUpTests : ItemBehaviourTestBase<HealthUpBehaviour>
    {
        protected override IItemConfig InitializeConfig() => new MockItemConfig();

        [Test]
        public void FullHP_HealsNothing()
        {
            Behaviour.OnPickup(Owner);
            Assert.AreEqual(100, Owner.Health.Value.Value);
        }

        [TestCase(90, 100)]
        [TestCase(70, 100)]
        [TestCase(50, 90)]
        [TestCase(30, 70)]
        [TestCase(10, 50)]
        public void NotFullHP_Heals_CapsAtMaxHP(float initial, float expectedAfterHeal)
        {
            Owner.Damageable.Damage(Owner.Health.Value - initial);
            Behaviour.OnPickup(Owner);
            Assert.AreEqual(expectedAfterHeal, Owner.Health.Value);
        }
    }
}
