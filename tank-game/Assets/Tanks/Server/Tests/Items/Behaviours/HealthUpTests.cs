using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class HealthUpTests : ItemBehaviourTestBase<HealthUpBehaviour>
    {
        protected override IItemConfig InitializeConfig() => new MockItemConfig();

        [Test]
        public void PickUp_Heals()
        {
            Behaviour.OnPickup(MockOwner);
            var health = MockOwner.GetBehaviour<IHealthBehaviour>().Require<IAuthoritativeHealth>();
            health.Received(1).Heal(40);
        }
    }
}
