using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Tests.Bootstrap
{
    public class GameRestarterTests
    {
        private GameRestarter _component;

        [SetUp]
        public void Setup()
        {
            _component = new GameRestarter(
                Substitute.For<IMessageBus>(), Substitute.For<IProjectilePoolService>());

            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [Test]
        public void DelayBeforeRestart()
        {
            _component.Behaviour.GetAlivePlayerCount().Returns(2);
            _component.Handle(new Died(), null);

            for (int tick = 1; tick <= 24; tick++) _component.Tick(tick, tickDelta: 0.1f);
            _component.Behaviour.DidNotReceiveWithAnyArgs().GetAlivePlayerCount();

            _component.Tick(100, 0.2f);
            _component.Behaviour.Received(1).GetAlivePlayerCount();
        }

        [Test]
        public void OthersRemain_Continues()
        {
            _component.Behaviour.GetAlivePlayerCount().Returns(2);
            _component.Handle(new Died(), null);
            _component.Tick(tick: 10, tickDelta: 500);
            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();
        }

        [TestCase(1)]
        [TestCase(0)]
        public void OneOrZeroRemain_Restarts(int playerCount)
        {
            _component.Behaviour.GetAlivePlayerCount().Returns(playerCount);
            _component.Handle(new Died(), null);
            _component.Tick(tick: 10, tickDelta: 500);
            _component.Behaviour.Received(1).PrepareGame();
        }
    }
}
