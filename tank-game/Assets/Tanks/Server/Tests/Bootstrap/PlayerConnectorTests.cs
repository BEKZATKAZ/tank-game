using NSubstitute;
using NUnit.Framework;
using Tanks.Core;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Teams;
using Unity.Netcode;

namespace Tanks.Server.Tests.Bootstrap
{
    public class PlayerConnectorTests
    {
        private IClientConnectionValidator _validator;
        private IConnectedClientService _clientService;
        private IVirtualMachineService _vmService;
        private ISessionSettingsService _settings;
        private IServerLifetimeService _lifetime;
        private ITeamBehaviourService _teamService;
        private PlayerConnector _component;

        [SetUp]
        public void Setup()
        {
            _validator = Substitute.For<IClientConnectionValidator>();
            _clientService = Substitute.For<IConnectedClientService>();
            _vmService = Substitute.For<IVirtualMachineService>();
            _settings = Substitute.For<ISessionSettingsService>();
            _lifetime = Substitute.For<IServerLifetimeService>();
            _teamService = Substitute.For<ITeamBehaviourService>();

            _component = new PlayerConnector(
                _validator, _clientService, _vmService, _settings, _lifetime, _teamService);

            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Initialize_StandsBy()
        {
            _vmService.Received(1).StandBy();
        }

        [Test]
        public void Approval_FullSession_Rejects()
        {
            _clientService.ConnectedClientCount.Returns(1);
            _component.OnClientConnected(1);

            var response = new NetworkManager.ConnectionApprovalResponse();
            _component.ApprovalCheck(default, response);

            Assert.AreEqual("session-already-started", response.Reason, "Reason dismatch");
            Assert.False(response.Approved, "Should reject");
        }

        [Test]
        public void Approval_ValidPayload_Approves()
        {
            byte[] data = { 1, 2, 3, 4, 5 };

            var request = new NetworkManager.ConnectionApprovalRequest { Payload = data };
            var response = new NetworkManager.ConnectionApprovalResponse();

            _validator.Validate(data).Returns(Result.Success<ClientConnectionPayload>(default));
            _component.ApprovalCheck(request, response);

            Assert.True(response.Approved, "Should approve");
        }

        [TestCase("some-error")]
        [TestCase("missing-username")]
        public void Approval_InvalidPayload_Rejects(string reason)
        {
            var response = new NetworkManager.ConnectionApprovalResponse();

            _validator.Validate(Arg.Any<byte[]>())
                .Returns(Result.Failure<ClientConnectionPayload>(reason));

            _component.ApprovalCheck(default, response);

            Assert.AreEqual(reason, response.Reason, "Reason dismatch");
            Assert.False(response.Approved, "Should reject");
        }

        [TestCase(1ul)]
        [TestCase(5ul)]
        [TestCase(10ul)]
        public void WaitsUntilEveryoneConnects(ulong desiredCount)
        {
            _settings.GetSettings().DesiredPlayerCount.Returns((int)desiredCount);

            for (ulong id = 1; id <= desiredCount; id++)
            {
                _component.OnClientConnected(id);
                _clientService.ConnectedClientCount.Returns((int)id);
            }

            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();
            _component.OnClientConnected(desiredCount + 1);

            _lifetime.Received(1).TrySetStep(GameStep.Session);
            _component.Behaviour.Received(1).PrepareGame();
        }

        [TestCase(1ul)]
        [TestCase(5ul)]
        [TestCase(10ul)]
        public void Disconnect_RemovesClient(ulong id)
        {
            _component.OnClientDisconnected(id);
            _clientService.Received(1).RemoveClient(id);
            _teamService.Received(1).TryGetTeamByPlayerId(id, out Arg.Any<Team>());
        }
    }
}
