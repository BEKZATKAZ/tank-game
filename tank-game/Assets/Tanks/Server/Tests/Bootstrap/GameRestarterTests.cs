using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Tests.Bootstrap
{
    public class GameRestarterTests
    {
        private IConnectedClientService _clientService;
        private GameRestarter _component;

        [SetUp]
        public void Setup()
        {
            _clientService = Substitute.For<IConnectedClientService>();

            _component = new GameRestarter(
                Substitute.For<IMessageBus>(),
                Substitute.For<IProjectilePoolService>(),
                _clientService);

            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void IgnoreIfNoPlayerWasEliminated()
        {
            AddPlayers(2);
            Handle(clientId: 0); // Id 0 = Not a player

            _component.Tick(100, tickDelta: 500);
            _clientService.DidNotReceiveWithAnyArgs().GetConnectedClientIds();
        }

        [Test]
        public void DelayBeforeRestart()
        {
            AddPlayers(2);
            Handle(1);

            for (int tick = 1; tick <= 24; tick++) _component.Tick(tick, tickDelta: 0.1f);
            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();

            _component.Tick(100, 0.2f);
            _component.Behaviour.Received(1).PrepareGame();
        }

        [TestCase(10)]
        [TestCase(6)]
        [TestCase(3)]
        public void OthersRemain_Continues(int playerCount)
        {
            AddPlayers(playerCount);
            Handle(1);
            _component.Tick(tick: 10, tickDelta: 500);
            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();
        }

        [TestCase(1)]
        [TestCase(0)]
        public void OneOrZeroRemain_Restarts(int playerCount)
        {
            AddPlayers(playerCount);
            Handle(1);
            _component.Tick(tick: 10, tickDelta: 500);
            _component.Behaviour.Received(1).PrepareGame();
        }

        private void Handle(ulong clientId)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            entity.OwnerClientId.Returns(clientId);
            _component.Handle(new Died { Target = entity }, null);
        }

        private void AddPlayers(int count)
        {
            ulong[] connected = new ulong[count];

            for (int i = 0; i < count; i++)
            {
                connected[i] = (ulong)(i + 1);
                ClientData data = new ClientData("test");
                _clientService.GetClientData(connected[i]).Returns(data);
            }

            _clientService.GetConnectedClientIds().Returns(connected);
            _clientService.ConnectedClientCount.Returns(count);
        }
    }
}
