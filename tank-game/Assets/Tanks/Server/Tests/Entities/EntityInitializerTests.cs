using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Shared.Entities;

namespace Tanks.Server.Tests.Entities
{
    public class EntityInitializerTests
    {
        [Test]
        public void Initialize_AssignsUniqueId()
        {
            EntityInitializer component = new EntityInitializer();
            component.Initialize(Substitute.For<IEntityBehaviour>());
            component.Behaviour.Received(1).SetId(Arg.Any<EntityId>());
        }
    }
}
