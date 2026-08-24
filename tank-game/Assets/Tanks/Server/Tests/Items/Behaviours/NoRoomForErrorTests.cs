using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Stats;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class NoRoomForErrorTests : ItemBehaviourTestBase<NoRoomForErrorBehaviour>
    {
        protected override IItemConfig InitializeConfig() => new MockItemConfig
        {
            StatsDictionary = new Dictionary<string, ItemStat>()
            {
                ["damage"] = new ItemStat
                {
                    ApplyOnPickUp = false,
                    Value = 99999,
                    ValueType = StatValueType.Percent
                }
            }
        };

        [Test]
        public void OneHP_DoesNotDealDamage()
        {
            Owner.Damageable.Damage(99);
            Behaviour.OnPickup(Owner);

            Assert.AreEqual(1, Owner.Health.Value.Value);
            Assert.Greater(Owner.StatOwner.GetStat(StatType.Damage, 1), 1,
                "Damage did not multiply");
        }

        [TestCase(1.1f)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(100)]
        public void AboveOneHP_DropsToOneHP(float initial)
        {
            if (initial < 100) Owner.Damageable.Damage(100 - initial);
            Behaviour.OnPickup(Owner);

            Assert.AreEqual(1, Owner.Health.Value.Value);
            Assert.Greater(Owner.StatOwner.GetStat(StatType.Damage, 1), 1,
                "Damage did not multiply");
        }
    }
}
