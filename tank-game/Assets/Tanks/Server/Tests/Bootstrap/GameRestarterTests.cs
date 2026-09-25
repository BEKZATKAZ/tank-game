using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Tests.Bootstrap
{
    public class GameRestarterTests
    {
        private ITeamBehaviourService _teamService;
        private ISessionSettingsService _settings;
        private IServerLifetimeService _lifetime;
        private GameRestarter _component;

        [SetUp]
        public void Setup()
        {
            _teamService = Substitute.For<ITeamBehaviourService>();
            _lifetime = Substitute.For<IServerLifetimeService>();

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().MaxScore.Returns(30);

            _component = new GameRestarter(
                Substitute.For<IMessageBus>(),
                Substitute.For<IProjectilePoolService>(),
                _teamService, _settings, _lifetime);

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
            _teamService.DidNotReceiveWithAnyArgs().GetAllTeams();
        }

        [Test]
        public void DelayBeforeRestart()
        {
            AddPlayers(2);
            Handle(1);

            for (int tick = 1; tick <= 14; tick++) _component.Tick(tick, tickDelta: 0.1f);
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

            _teamService
                .DidNotReceiveWithAnyArgs()
                .AddTeamScoreEveryoneRpc(Arg.Any<TeamId>(), Arg.Any<int>());
        }

        [Test]
        public void OneRemains_Restarts()
        {
            AddPlayers(2);
            Handle(clientId: 1);

            Team team = _teamService.GetTeamByPlayerId(2);
            _component.Tick(tick: 10, tickDelta: 500);

            _teamService.Received(1).AddTeamScoreEveryoneRpc(team.Id, 1);
            _component.Behaviour.Received(1).PrepareGame();
        }

        [Test]
        public void TeamWins_EndsGame()
        {
            AddPlayers(2);

            Team winningTeam = _teamService.GetTeamByPlayerId(1);
            winningTeam.AddScore(_settings.GetSettings().MaxScore);

            Handle(clientId: 2);

            _component.Tick(tick: 10, tickDelta: 500);
            _component.Tick(tick: 11, tickDelta: 600);

            _lifetime.Received(1).TrySetStep(GameStep.End);
            _component.Behaviour.Received(1).EndGameEveryoneRpc(winningTeam.Id);
            _component.Behaviour.DidNotReceiveWithAnyArgs().PrepareGame();
        }

        [Test]
        public void NoOneRemains_Restarts()
        {
            AddPlayers(1);
            Handle(1);

            _component.Tick(tick: 10, tickDelta: 500);
            _component.Behaviour.Received(1).PrepareGame();

            _teamService
                .DidNotReceiveWithAnyArgs()
                .AddTeamScoreEveryoneRpc(Arg.Any<TeamId>(), Arg.Any<int>());
        }

        private void Handle(ulong clientId)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            entity.OwnerClientId.Returns(clientId);

            if (clientId > 0)
            {
                _teamService.GetTeamByPlayerId(clientId).GetPlayer(clientId).SetEliminated(true);
            }

            _component.Handle(new Died { Target = entity }, null);
        }

        private void AddPlayers(int count)
        {
            Team[] teams = new Team[count];

            for (int i = 0; i < count; i++)
            {
                ulong ownerClientId = (ulong)(i + 1);
                TeamId teamId = new TeamId((ushort)(i + 101));

                teams[i] = new Team(teamId);
                teams[i].AddPlayer(ownerClientId, "test");

                _teamService.GetTeamByPlayerId(ownerClientId).Returns(teams[i]);
            }

            _teamService.GetAllTeams().Returns(teams);
        }
    }
}
