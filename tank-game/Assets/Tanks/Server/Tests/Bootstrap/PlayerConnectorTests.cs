using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;

namespace Tanks.Server.Tests.Bootstrap
{
    public class PlayerConnectorTests
    {
        private PlayerConnector _component;

        [SetUp]
        public void Setup()
        {
            _component = new PlayerConnector();
            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [TestCase(1ul)]
        [TestCase(5ul)]
        [TestCase(10ul)]
        public void WaitsUntilEveryoneConnects(ulong desiredCount)
        {
            List<ulong> connected = new List<ulong>();

            _component.Behaviour.DesiredPlayerCount.Returns((int)desiredCount);
            _component.Behaviour.GetConnectedClientIds().Returns(connected);

            for (ulong id = 1; id <= desiredCount; id++)
            {
                _component.OnClientConnected(id);
                connected.Add(id);
            }

            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();
            _component.OnClientConnected(desiredCount + 1);
            _component.Behaviour.Received(1).PrepareGame();
        }
    }
}
