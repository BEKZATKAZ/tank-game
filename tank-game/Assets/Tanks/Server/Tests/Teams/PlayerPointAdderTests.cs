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

        [TestCase(true)]
        [TestCase(false)]
        public void AssistedElimination_AddsAndSyncsOneAssistIncludingTeamkills(bool teamkill)
        {
            Team victimTeam = CreateTeam(101);
            Team killerTeam = teamkill ? victimTeam : CreateTeam(102);
            IEntityBehaviour victim = CreateEntity(1, victimTeam);
            IEntityBehaviour killer = CreateEntity(2, killerTeam);
            IEntityBehaviour helper = CreateEntity(3, victimTeam);

            _component.Handle(new Died { Target = victim, From = killer, Assistant = helper });

            DidAddPoints(3, 1, PlayerPointType.Assistance, victimTeam);
            Assert.AreEqual(0, victimTeam.GetPlayer(3).Eliminations);
            DidAddPoints(1, 1, PlayerPointType.Deaths, victimTeam);
            DidAddPoints(2, teamkill ? -1 : 1, PlayerPointType.Eliminations, killerTeam);
        }

        [TestCase("environment")]
        [TestCase("suicide")]
        [TestCase("killer")]
        [TestCase("victim")]
        [TestCase("invalid")]
        [TestCase("missing")]
        public void IneligibleAssistant_DoesNotReceivePoints(string reason)
        {
            Team team = CreateTeam(101);
            IEntityBehaviour victim = CreateEntity(1, team);
            IEntityBehaviour killer = CreateEntity(2, team);
            IEntityBehaviour helper = CreateEntity(3, team);

            switch (reason)
            {
                case "environment": killer = null; break;
                case "suicide": killer = victim; break;
                case "killer": helper = killer; break;
                case "victim": helper = victim; break;
                case "invalid": helper.IsValid.Returns(false); break;
                case "missing": helper = null; break;
            }

            _component.Handle(new Died { Target = victim, From = killer, Assistant = helper });

            foreach (PlayerState player in team.Players)
            {
                Assert.AreEqual(0, player.Assistance);
            }
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
