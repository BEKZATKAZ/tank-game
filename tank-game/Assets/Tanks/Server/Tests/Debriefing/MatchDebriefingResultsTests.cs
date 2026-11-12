using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Server.Bootstrap;
using Tanks.Server.Debriefing;
using Tanks.Server.Tests.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using UnityEngine.TestTools;

namespace Tanks.Server.Tests.Debriefing
{
    public sealed class MatchDebriefingResultsTests
    {
        [UnityTest]
        public IEnumerator CompletedMatch_KeepsRosterAndWaitsForAcknowledgment()
        {
            return VerifyCompletion(acknowledge: true, disconnect: false);
        }

        [UnityTest]
        public IEnumerator AcknowledgmentTimeout_ReleasesShutdown()
        {
            return VerifyCompletion(acknowledge: false, disconnect: false);
        }

        [UnityTest]
        public IEnumerator DisconnectDuringDelivery_ReleasesShutdown()
        {
            return VerifyCompletion(acknowledge: false, disconnect: true);
        }

        private IEnumerator VerifyCompletion(bool acknowledge, bool disconnect)
        {
            ITeamBehaviourService teams = Substitute.For<ITeamBehaviourService>();
            IDebriefingService backend = Substitute.For<IDebriefingService>();
            var lifetime = Substitute.For<IServerLifetimeService>();
            IMessageBus bus = Substitute.For<IMessageBus>();
            IGameStartBehaviour behaviour = Substitute.For<IGameStartBehaviour>();

            Team winner = new(new TeamId(1));
            Team loser = new(new TeamId(2));
            ConnectedClientService clients = new();
            DebriefingTestClockService clock = new();
            MatchDebriefingSettings settings = new();

            PlayerDebriefingPayload[] captured = null;
            Task finalization = null;

            PlayerState connected = winner.AddPlayer(1, "Connected");
            PlayerState disconnected = loser.AddPlayer(2, "Disconnected");

            connected.OverridePoints(3, 2, 1);
            disconnected.OverridePoints(1, 1, 2);
            winner.AddScore(3);
            loser.AddScore(1);

            teams.GetAllTeams().Returns(new[] { winner, loser });

            clients.AddClient(1, new ClientData("Connected", playerId: "backend-1"));

            clients.AddClient(2, new ClientData("Disconnected",
                playerId: "backend-2"));

            backend.SubmitAsync("match", Arg.Any<PlayerDebriefingPayload[]>())
                .Returns(call =>
                {
                    captured = call.Arg<PlayerDebriefingPayload[]>();

                    DebriefingPlayerResult[] conclusions = captured
                        .Select(SuccessfulResult)
                        .ToArray();

                    return Result.Success(new DebriefingResponse
                    {
                        Conclusions = conclusions
                    });
                });

            lifetime.When(value => value.HoldShutdown(Arg.Any<Task>()))
                .Do(call => { finalization = call.Arg<Task>(); });

            MatchDebriefingSubmissionService submission = new(backend, clock);

            MatchDebriefingResults component = new(
                bus, teams, clients, submission, clock, lifetime, settings);

            component.Initialize(behaviour);
            component.Handle(new GameStartedFromServer(), null);
            clients.RemoveClient(2);
            loser.RemovePlayer(2);

            EndGameFromServer end = new EndGameFromServer
            {
                MatchId = "match", WinningTeamId = winner.Id
            };

            component.Handle(end, null);
            component.Handle(end, null);

            Assert.That(captured.Length, Is.EqualTo(2));
            Assert.That(captured[0].Score, Is.EqualTo(winner.Score.Value));
            Assert.That(captured[0].Assists, Is.EqualTo(connected.Assistance));
            Assert.That(captured[1].Deaths, Is.EqualTo(disconnected.Deaths));
            Assert.That(captured[0].IsVictory, Is.True);
            Assert.That(captured[1].IsVictory, Is.False);
            Assert.That(finalization.IsCompleted, Is.False);
            behaviour.Received(1).SendDebriefing(1, "match", Arg.Any<string>());

            behaviour.DidNotReceive().SendDebriefing(2,
                Arg.Any<string>(), Arg.Any<string>());

            lifetime.Received(1).HoldShutdown(Arg.Any<Task>());

            component.Handle(new DebriefingAcknowledged
            {
                ClientId = 1, MatchId = "other"
            });

            Assert.That(finalization.IsCompleted, Is.False);

            if (acknowledge) component.Handle(new DebriefingAcknowledged
            {
                ClientId = 1, MatchId = "match"
            });

            if (disconnect) clients.RemoveClient(1);

            clock.Advance(acknowledge || disconnect ? 0.05f :
                settings.AcknowledgmentTimeoutSeconds);

            yield return DebriefingTestTasks.Complete(finalization);
            component.Destroy();
        }

        [Test]
        public void NoCompletedMatch_DoesNotSubmitOrHoldShutdown()
        {
            IDebriefingService backend = Substitute.For<IDebriefingService>();
            var lifetime = Substitute.For<IServerLifetimeService>();
            IMessageBus bus = Substitute.For<IMessageBus>();
            ITeamBehaviourService teams = Substitute.For<ITeamBehaviourService>();
            IGameStartBehaviour behaviour = Substitute.For<IGameStartBehaviour>();

            DebriefingTestClockService clock = new();
            ConnectedClientService clients = new();
            MatchDebriefingSettings settings = new();
            MatchDebriefingSubmissionService submission = new(backend, clock);

            MatchDebriefingResults component = new(
                bus, teams, clients, submission, clock, lifetime, settings);

            component.Initialize(behaviour);
            component.Destroy();
            lifetime.DidNotReceive().HoldShutdown(Arg.Any<Task>());
            Assert.That(backend.ReceivedCalls(), Is.Empty);
        }

        private static DebriefingPlayerResult SuccessfulResult(
            PlayerDebriefingPayload player)
        {
            return new DebriefingPlayerResult
            {
                PlayerId = player.PlayerId,
                Value = new PlayerDebriefing { MatchId = "match" }
            };
        }
    }
}
