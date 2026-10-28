using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Teams;

namespace Tanks.Server.Tests.Entities
{
    public class HealthManagerTests
    {
        private AuthoritativeHealth _component;
        private IEntityBehaviour _victim;

        [SetUp]
        public void Setup()
        {
            IHealthBehaviour behaviour = Substitute.For<IHealthBehaviour>();
            behaviour.InitialHealth.Returns(100u);

            _victim = CreatePlayer(1);
            behaviour.Entity.Returns(_victim);

            _component = new AuthoritativeHealth();
            _component.Initialize(behaviour);
        }

        [TestCase(1u)]
        [TestCase(45u)]
        [TestCase(90u)]
        public void Damage_ReducesHP(uint amount)
        {
            uint max = _component.GetState().Max;
            _component.Damage(amount);

            HealthState expected = new HealthState(100 - (int)amount, max);
            HealthState actual = _component.GetState();

            Assert.AreEqual(expected, actual, "Wrong state");
            _component.Tick(1, 0.1f);
            _component.Behaviour.Received(1).UpdateStateEveryoneRpc(actual);
        }

        [TestCase(9.99f, true)]
        [TestCase(10, true)]
        [TestCase(10.01f, false)]
        public void Damage_AssistWindow_UsesLastDamagingHit(float delay, bool expectedAssist)
        {
            IEntityBehaviour helper = CreatePlayer(2);
            IEntityBehaviour killer = CreatePlayer(3);

            _component.Damage(10, helper);
            _component.Tick(1, delay);
            _component.Damage(90, killer);

            AssertAssistant(killer, expectedAssist ? helper : null);
        }

        [Test]
        public void Damage_MultipleHelpers_UsesMostRecentHitIncludingSameTime()
        {
            IEntityBehaviour first = CreatePlayer(2);
            IEntityBehaviour second = CreatePlayer(3);
            IEntityBehaviour killer = CreatePlayer(4);

            _component.Damage(10, first);
            _component.Damage(10, second);
            _component.Damage(10, first);
            _component.Damage(70, killer);

            AssertAssistant(killer, first);
        }

        [Test]
        public void Damage_RepeatedHit_RefreshesAssistWindow()
        {
            IEntityBehaviour helper = CreatePlayer(2);
            IEntityBehaviour killer = CreatePlayer(3);

            _component.Damage(10, helper);
            _component.Tick(1, 9);

            _component.Damage(10, helper);
            _component.Tick(1, 18);

            _component.Damage(80, killer);
            AssertAssistant(killer, helper);
        }

        [TestCase(0u)]
        [TestCase(1u)]
        public void Damage_ZeroDamageDoesNotRefreshAssist(uint amount)
        {
            IEntityBehaviour helper = CreatePlayer(2);
            IEntityBehaviour killer = CreatePlayer(3);

            _component.Damage(10, helper);
            _component.Tick(1, 9);

            _component.Damage(amount, helper);
            _component.Tick(1, 11);

            _component.Damage(100, killer);
            AssertAssistant(killer, amount > 0 ? helper : null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Damage_SuicideOrEnvironment_HasNoAssist(bool suicide)
        {
            _component.Damage(10, CreatePlayer(2));
            IEntityBehaviour killer = suicide ? _victim : null;

            _component.Damage(90, killer);
            AssertAssistant(killer, null);
        }

        [Test]
        public void Damage_InvalidLatestHelper_FallsBackToEarlierHelper()
        {
            IEntityBehaviour earlier = CreatePlayer(2);
            IEntityBehaviour latest = CreatePlayer(3);
            IEntityBehaviour killer = CreatePlayer(4);

            _component.Damage(10, earlier);
            _component.Damage(10, latest);

            latest.IsValid.Returns(false);
            _component.Damage(80, killer);

            AssertAssistant(killer, earlier);
        }

        [Test]
        public void Damage_KillerAndVictimHistory_DoNotCountAsAssist()
        {
            IEntityBehaviour killer = CreatePlayer(2);

            _component.Damage(10, killer);
            _component.Damage(10, _victim);
            _component.Damage(80, killer);

            AssertAssistant(killer, null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Damage_NonPlayerSourceOrTarget_HasNoAssist(bool nonPlayerTarget)
        {
            IEntityBehaviour killer = CreatePlayer(3);
            _component.Damage(10, CreatePlayer(2));

            IEntityBehaviour nonPlayer = Substitute.For<IEntityBehaviour>();
            nonPlayer.IsValid.Returns(true);
            nonPlayer.GetBehaviour<ITeamParticipantBehaviour>().Returns((ITeamParticipantBehaviour)null);
            
            if (nonPlayerTarget) _component.Behaviour.Entity.Returns(nonPlayer);
            else killer = nonPlayer;

            _component.Damage(90, killer);
            AssertAssistant(killer, null);
        }

        [TestCase("death")]
        [TestCase("disable")]
        [TestCase("destroy")]
        [TestCase("initialize")]
        [TestCase("set-dead")]
        [TestCase("despawn")]
        public void Damage_NewLifeOrLifecycleReset_ClearsHistory(string reset)
        {
            IEntityBehaviour killer = CreatePlayer(3);
            _component.Damage(10, CreatePlayer(2));

            switch (reset)
            {
                case "death": _component.Damage(90, killer); break;
                case "disable": _component.IsEnabled = false; _component.IsEnabled = true; break;
                case "destroy": _component.Destroy(); break;
                case "initialize": _component.Initialize(_component.Behaviour); break;
                case "set-dead": _component.SetState(new HealthState(0, 100)); break;
                case "despawn": _component.Handle(new HealthDespawned()); break;
            }

            _component.SetState(new HealthState(100, 100));
            _component.Behaviour.ClearReceivedCalls();
            _component.Damage(100, killer);

            AssertAssistant(killer, null);
        }

        [Test]
        public void Damage_AlreadyDead_DoesNotPublishAnotherDeath()
        {
            _component.Damage(100, CreatePlayer(2));
            _component.Behaviour.ClearReceivedCalls();

            _component.Damage(1, CreatePlayer(3));

            _component.Behaviour.DidNotReceiveWithAnyArgs().UpdateStateEveryoneRpc(default);
        }

        [Test]
        public void Initialize_RegistersDespawnHandler()
        {
            _component.Behaviour.Received(1).RegisterMessageHandler<HealthDespawned>(_component);
        }

        [Test]
        public void Heal_DoesNotExtendAssistWindow()
        {
            IEntityBehaviour helper = CreatePlayer(2);
            IEntityBehaviour killer = CreatePlayer(3);

            _component.Damage(10, helper);
            _component.Tick(1, 9);

            _component.Heal(10, helper);
            _component.Tick(1, 11);

            _component.Damage(100, killer);
            AssertAssistant(killer, null);
        }

        [Test]
        public void Damage_DifferentVictims_HaveIndependentAssistHistory()
        {
            IEntityBehaviour otherVictim = CreatePlayer(4);
            IHealthBehaviour behaviour = Substitute.For<IHealthBehaviour>();

            behaviour.InitialHealth.Returns(100u);
            behaviour.Entity.Returns(otherVictim);

            AuthoritativeHealth other = new AuthoritativeHealth();
            other.Initialize(behaviour);

            IEntityBehaviour helper = CreatePlayer(2);
            IEntityBehaviour killer = CreatePlayer(3);
            _component.Damage(10, helper);

            other.Damage(100, killer);
            _component.Damage(90, killer);

            AssertAssistant(killer, helper);
            behaviour.Received(1).UpdateStateEveryoneRpc(other.GetState(), killer.Id);
        }

        [TestCase(1)]
        [TestCase(45)]
        [TestCase(90)]
        [TestCase(1000)]
        public void Heal_RestoresHPUpToMax(int amount)
        {
            _component.SetState(new HealthState(50, 150));
            _component.Heal((uint)amount);

            HealthState expected = new HealthState(50 + amount, 150);
            HealthState actual = _component.GetState();

            Assert.AreEqual(expected, actual, "Wrong state");
            _component.Tick(5, 0.1f);
            _component.Behaviour.Received(1).UpdateStateEveryoneRpc(actual);
        }

        private void AssertAssistant(IEntityBehaviour killer, IEntityBehaviour helper)
        {
            _component.Behaviour.Received(1).UpdateStateEveryoneRpc(
                _component.GetState(), killer?.Id ?? default, default, helper?.Id ?? default);
        }

        private static IEntityBehaviour CreatePlayer(int id)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            ITeamParticipantBehaviour participant = Substitute.For<ITeamParticipantBehaviour>();
            participant.State.Returns(new PlayerState((ulong)id, $"Player {id}", default));

            entity.Id.Returns(new EntityId(id));
            entity.IsValid.Returns(true);
            entity.GetBehaviour<ITeamParticipantBehaviour>().Returns(participant);
            return entity;
        }
    }
}
