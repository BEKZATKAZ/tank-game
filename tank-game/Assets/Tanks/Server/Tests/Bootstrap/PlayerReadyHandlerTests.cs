using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Server.Bootstrap;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using UnityEngine;

namespace Tanks.Server.Tests.Bootstrap
{
    public class PlayerReadyHandlerTests
    {
        private IConnectedClientService _clientService;
        private PlayerReadyHandler _component;

        [SetUp]
        public void Setup()
        {
            _clientService = Substitute.For<IConnectedClientService>();
            _component = new PlayerReadyHandler(_clientService);
            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(10)]
        public void WaitForPlayers_AddsPendingRequests(int playerCount)
        {
            _clientService.GetConnectedClientIds().Returns(
                Enumerable.Range(1, playerCount).Select(x => (ulong)x));

            _component.Handle(new WaitingForPlayersRequested());

            Assert.AreEqual(playerCount, _component.Pending.Count, "Player count dismatch");

            for (ulong id = 1; id < (ulong)playerCount; id++)
            {
                int secret = _component.Pending.First();

                _component.Behaviour.RequestReadyConfirmation(id, secret);
                _component.Handle(new PlayerReadyConfirmed { SecretHandle = secret });

                int expectedPending = playerCount - (int)id;
                int actualPending = _component.Pending.Count;

                Assert.AreEqual(expectedPending, actualPending,
                    "Pending requests should decrease");
            }

            _component.Behaviour
                .DidNotReceiveWithAnyArgs()
                .StartGameEveryoneRpc(Arg.Any<GameStartedFromServer>());

            int lastSecret = _component.Pending.First();
            _component.Handle(new PlayerReadyConfirmed { SecretHandle = lastSecret });

            Assert.AreEqual(0, _component.Pending.Count,
                "Everyone should be ready");

            _component.Behaviour
                .Received(1)
                .StartGameEveryoneRpc(Arg.Any<GameStartedFromServer>());
        }
    }
}