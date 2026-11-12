using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Text;
using System.Threading.Tasks;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Debriefing;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Bootstrap;
using Tanks.Server.Debriefing;
using Tanks.Server.Tests.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Unity.Netcode;

namespace Tanks.Server.Tests.Debriefing
{
    public sealed class DirectSceneDebriefingTests
    {
        [Test]
        public void DirectSceneClients_JoinWithoutSubmittingOrDeliveringRewards()
        {
            ITeamBehaviourService teams = Substitute.For<ITeamBehaviourService>();
            IDebriefingService backend = Substitute.For<IDebriefingService>();
            var lifetime = Substitute.For<IServerLifetimeService>();
            IMessageBus bus = Substitute.For<IMessageBus>();
            IGameStartBehaviour behaviour = Substitute.For<IGameStartBehaviour>();
            var settings = Substitute.For<ISessionSettingsService>();

            ConnectedClientService clients = new();
            Team team = new(new TeamId(1));
            DebriefingTestClockService clock = new();
            MatchDebriefingSettings debriefingSettings = new();

            teams.GetAllTeams().Returns(new[] { team });

            PlayerConnector connector = new(new ClientConnectionValidator(),
                clients, settings, lifetime, teams);

            for (ulong id = 1; id <= 2; id++)
            {
                GameplaySessionService session = new();

                session.Begin(true, null);

                ClientConnectionPayload payload = new()
                {
                    PlayerId = session.PlayerId,
                    DisplayName = "Editor client",
                    IsDirectSceneTest = true
                };

                byte[] data = Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(payload));

                NetworkManager.ConnectionApprovalResponse response = new();

                connector.ApprovalCheck(new()
                {
                    ClientNetworkId = id, Payload = data
                }, response);

                Assert.That(response.Approved, Is.True);
                team.AddPlayer(id, payload.DisplayName);
            }

            MatchDebriefingSubmissionService submission = new(backend, clock);

            MatchDebriefingResults component = new(bus, teams, clients,
                submission, clock, lifetime, debriefingSettings);

            component.Initialize(behaviour);
            component.Handle(new GameStartedFromServer(), null);
            component.Handle(new EndGameFromServer { MatchId = "match" }, null);

            Assert.That(clients.ConnectedClientCount, Is.EqualTo(2));
            Assert.That(backend.ReceivedCalls(), Is.Empty);

            lifetime.DidNotReceive().HoldShutdown(Arg.Any<Task>());

            behaviour.DidNotReceive().SendDebriefing(
                Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<string>());

            component.Destroy();
        }
    }
}
