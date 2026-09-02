using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Shared.Entities;

namespace Tanks.Client.Tests.Entities
{
    public class ClientEntityInitializerTests
    {
        private IClientEntityService _service;
        private ClientEntityInitializer _component;

        [SetUp]
        public void Setup()
        {
            _service = new ClientEntityService();
            _component = new ClientEntityInitializer(_service);
        }

        [Test]
        public void Initialize_LocalPlayer_SetsLocalPlayer()
        {
            IEntityBehaviour behaviour = Substitute.For<IEntityBehaviour>();
            behaviour.IsLocalPlayer.Returns(true);

            _component.Initialize(behaviour);
            Assert.AreEqual(behaviour, _service.GetPossessedEntity());
        }

        [Test]
        public void Initialize_OtherPlayer_DoesNotSetLocalPlayer()
        {
            IEntityBehaviour behaviour = Substitute.For<IEntityBehaviour>();
            behaviour.IsLocalPlayer.Returns(false);

            _component.Initialize(behaviour);
            Assert.Null(_service.GetPossessedEntity());
        }
    }
}
