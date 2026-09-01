using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;

namespace Tanks.Shared.Tests.Entities
{
    public class EntityServiceTests
    {
        [Test]
        public void Add_AddsToEntityList()
        {
            IEntityService service = new EntityService();
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();

            EntityId id = new EntityId(10);
            EntityId otherId = new EntityId(11);

            entity.Id.Returns(id);
            Assert.Null(service.GetById(entity.Id), "Should return null when not registered");
            service.AddEntity(entity);

            Assert.AreEqual(entity, service.GetById(entity.Id));
            Assert.Null(service.GetById(otherId),
                "Should reutnr null as other entities were not registered");

            service.RemoveEntity(entity);
            Assert.Null(service.GetById(entity.Id), "Should return null when removed");
        }
    }
}
