using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Bootstrap;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Tests.Bootstrap
{
    public sealed class MatchLeaderboardResultsTests
    {
        private MatchLeaderboardResults _component;
        private IGameStartBehaviour _behaviour;
        private IItemOwnerBehaviour _owner;
        private PlayerState _player;

        [SetUp]
        public void Setup()
        {
            var team = new Team(new TeamId(101));
            _player = team.AddPlayer(1, "Player");
            var teams = Substitute.For<ITeamBehaviourService>();
            teams.GetAllTeams().Returns(new[] { team });
            teams.TryGetTeamByPlayerId(1, out Arg.Any<Team>()).Returns(x =>
            {
                x[1] = team;
                return true;
            });

            _owner = Substitute.For<IItemOwnerBehaviour>();
            _owner.OwnerClientId.Returns(1UL);
            _owner.IsServer.Returns(true);
            _behaviour = Substitute.For<IGameStartBehaviour>();
            _component = new MatchLeaderboardResults(Substitute.For<IMessageBus>(), teams);
            _component.Initialize(_behaviour);
        }

        [TearDown]
        public void TearDown() => _component.Destroy();

        [Test]
        public void Completion_SendsFinalScoreAndPickupsAcrossAllRoundsOnlyOnce()
        {
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);
            _component.Handle(new GameStartedFromServer(), null);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);
            _component.Handle(new GameStartedFromServer(), null);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);
            _player.AddPoints(PlayerPointType.Eliminations, -1);

            _component.Handle(new EndGameFromServer(), null);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);
            _component.Handle(new EndGameFromServer(), null);

            _behaviour.Received(1).SendLeaderboardResult(1,
                Arg.Is<LeaderboardMatchResult>(x => x.Eliminations == -1 && x.PickedUpItems == 2));
        }

        [Test]
        public void UnfinishedMatch_DoesNotSendResults()
        {
            _component.Handle(new GameStartedFromServer(), null);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);

            _behaviour.DidNotReceiveWithAnyArgs().SendLeaderboardResult(default, default);
        }

        [Test]
        public void Pickup_NonServerOrNonPlayerIsIgnored()
        {
            _component.Handle(new GameStartedFromServer(), null);
            _owner.IsServer.Returns(false);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);
            _owner.IsServer.Returns(true);
            _owner.OwnerClientId.Returns(99UL);
            _component.Handle(new ItemPickedUp { Owner = _owner }, null);

            _component.Handle(new EndGameFromServer(), null);

            _behaviour.Received(1).SendLeaderboardResult(1,
                Arg.Is<LeaderboardMatchResult>(x => x.PickedUpItems == 0));
        }
    }
}
