using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Teams;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Tests.Teams
{
    public class PlayerPointAdderTests
    {
        private PlayerPointAdder _component;

        [SetUp]
        public void Setup()
        {
            _component = new PlayerPointAdder(Substitute.For<IMessageBus>());
            _component.Initialize(Substitute.For<ITeamBehaviourService>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void NeutralElimination_AddsPoint()
        {
            Team team = CreateTeam(1);

            _component.Handle(new Died
            {
                Target = CreateEntity(playerId: 1, team),
                From = null
            });

            DidAddPoints(playerId: 1, amount: 1, PlayerPointType.Deaths, team);
        }

        [Test]
        public void EnemyElimination_AddsPoint()
        {
            Team targetTeam = CreateTeam(101);
            Team fromTeam = CreateTeam(102);

            _component.Handle(new Died
            {
                Target = CreateEntity(playerId: 1, targetTeam),
                From = CreateEntity(playerId: 2, fromTeam)
            });

            DidAddPoints(playerId: 1, amount: 1, PlayerPointType.Deaths, targetTeam);
            DidAddPoints(playerId: 2, amount: 1, PlayerPointType.Eliminations, fromTeam);
        }

        [Test]
        public void FriendlyElimination_RemovesOnePoint()
        {
            Team team = CreateTeam(101);

            _component.Handle(new Died
            {
                Target = CreateEntity(playerId: 1, team),
                From = CreateEntity(playerId: 2, team)
            });

            DidAddPoints(playerId: 1, amount: +1, PlayerPointType.Deaths, team);
            DidAddPoints(playerId: 2, amount: -1, PlayerPointType.Eliminations, team);
        }

        private void DidAddPoints(ulong playerId, int amount, PlayerPointType type, Team team)
        {
            Assert.AreEqual(amount, team.GetPlayer(playerId).GetPoints(type));

            _component.Behaviour
                .Received(1)
                .SyncPlayerState(team.GetPlayer(playerId));
        }

        private Team CreateTeam(ushort id)
        {
            return new Team(new TeamId(id));
        }

        private IEntityBehaviour CreateEntity(ulong playerId, Team team)
        {
            PlayerState state = team.AddPlayer(playerId, "test");

            var entity = Substitute.For<IEntityBehaviour>();
            var participant = entity.GetBehaviour<ITeamParticipantBehaviour>();

            entity.IsValid.Returns(true);
            participant.BelongsTo.Returns(team);
            participant.State.Returns(state);

            return entity;
        }
    }
}
