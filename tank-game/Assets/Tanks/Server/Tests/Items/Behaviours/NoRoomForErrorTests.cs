using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Stats;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;
using Tanks.Shared.Entities.Health;
using Tanks.Server.Entities;
using NSubstitute;

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
        public void PickUp_DropsToOneHP()
        {
            Behaviour.OnPickup(MockOwner);

            var health = MockOwner.GetBehaviour<IHealthBehaviour>().Require<IAuthoritativeHealth>();
            var statOwner = MockOwner.GetBehaviour<IStatOwnerBehaviour>();

            health.Received(1).SetState(new HealthState(1, 1));
            statOwner.Received(1).AddStat(StatType.Damage, StatValueType.Percent, 99999);
        }
    }
}
