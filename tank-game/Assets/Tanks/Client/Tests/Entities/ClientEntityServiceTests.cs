using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;

namespace Tanks.Client.Tests.Entities
{
    public class ClientEntityServiceTests
    {
        [Test]
        public void LocalPlayer_SetsCorrectly()
        {
            IClientEntityService service = new ClientEntityService();
            Assert.Null(service.GetPossessedEntity(), "Should be null when just created");

            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            service.PossessEntity(entity.AsRequired());
            Assert.AreEqual(entity, service.GetPossessedEntity());
        }
    }
}
