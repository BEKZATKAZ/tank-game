using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.UI.Gameplay.Killfeed;

namespace Tanks.UI.Tests.Gameplay
{
    public class KillfeedTests : ViewModelTestBase<KillfeedViewModel>
    {
        private IMessageBus _bus;

        protected override KillfeedViewModel CreateViewModel()
        {
            _bus = Substitute.For<IMessageBus>();
            return new KillfeedViewModel(_bus);
        }

        [Test]
        public void Constructor_RegistersHandler()
        {
            _bus.Received(1).RegisterHandler<Died>(ViewModel);
        }

        [Test]
        public void Dispose_UnregistersHandler()
        {
            KillfeedViewModel viewModel = new KillfeedViewModel(_bus);
            viewModel.Dispose();

            _bus.Received(1).UnregisterHandler<Died>(viewModel);
        }

        [Test]
        public void BusPublication_AddsEntriesUntilDisposed()
        {
            MessageBus bus = new MessageBus();
            KillfeedViewModel viewModel = new KillfeedViewModel(bus);
            Died message = new Died { Target = CreateEntity("Victim") };

            bus.Publish(message, null);
            Assert.AreEqual(1, viewModel.Entries.Value.Count);

            viewModel.Dispose();
            bus.Publish(message, null);
            Assert.AreEqual(1, viewModel.Entries.Value.Count);
        }

        [Test]
        public void Handle_NoParticipants_IgnoresDeath()
        {
            ViewModel.Handle(new Died { From = CreateEntity(), Target = CreateEntity() }, null);
            ViewModel.Handle(new Died(), null);

            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Handle_TwoPlayers_SnapshotsBothNames()
        {
            IEntityBehaviour from = CreateEntity("Killer");
            IEntityBehaviour target = CreateEntity("Victim");
            ViewModel.Handle(new Died { From = from, Target = target }, null);

            from.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(1, "Changed", default));

            Assert.AreEqual("Killer", ViewModel.Entries.Value[0].From);
            Assert.AreEqual("Victim", ViewModel.Entries.Value[0].Target);
        }

        [Test]
        public void Handle_SelfKill_RepeatsPlayerName()
        {
            IEntityBehaviour player = CreateEntity("Self");
            ViewModel.Handle(new Died { From = player, Target = player }, null);

            Assert.AreEqual("Self", ViewModel.Entries.Value[0].From);
            Assert.AreEqual("Self", ViewModel.Entries.Value[0].Target);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Handle_OnlyTargetIsPlayer_ShowsTargetBeforeSkull(bool environment)
        {
            ViewModel.Handle(new Died
            {
                From = environment ? null : CreateEntity(),
                Target = CreateEntity("Victim")
            }, null);

            Assert.AreEqual("Victim", ViewModel.Entries.Value[0].From);
            Assert.IsNull(ViewModel.Entries.Value[0].Target);
        }

        [Test]
        public void Handle_OnlySourceIsPlayer_Ignores()
        {
            ViewModel.Handle(new Died
            {
                From = CreateEntity("Killer"),
                Target = null
            }, null);

            Assert.AreEqual(0, ViewModel.Entries.Value.Count);
        }

        [Test]
        public void Handle_RepeatedDeath_AddsSeparateEntriesAndNotifiesBindings()
        {
            Died message = new Died { Target = CreateEntity("Victim") };
            int updates = 0;
            ViewModel.Entries.Bind(_ => updates++);

            ViewModel.Handle(message, null);
            ViewModel.Handle(message, null);

            Assert.AreEqual(2, ViewModel.Entries.Value.Count);
            Assert.AreEqual(3, updates);
            Assert.AreNotSame(ViewModel.Entries.Value[0], ViewModel.Entries.Value[1]);
        }

        [Test]
        public void Handle_NewDeath_PreservesExistingEntryIdentity()
        {
            Died message = new Died { Target = CreateEntity("Victim") };
            ViewModel.Handle(message, null);
            KillfeedEntry first = ViewModel.Entries.Value[0];

            ViewModel.Handle(message, null);

            Assert.AreSame(first, ViewModel.Entries.Value[1]);
            ViewModel.Tick(1f);
            Assert.AreSame(first, ViewModel.Entries.Value[1]);
        }

        [Test]
        public void Handle_SixDeaths_KeepsFiveNewestInOrder()
        {
            for (int i = 0; i < 6; i++)
            {
                ViewModel.Handle(new Died { Target = CreateEntity(i.ToString()) }, null);
            }

            Assert.AreEqual(5, ViewModel.Entries.Value.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual((5 - i).ToString(), ViewModel.Entries.Value[i].From);
            }
        }

        [Test]
        public void Tick_ExpiresEachEntryAfterFiveSeconds()
        {
            ViewModel.Handle(new Died { Target = CreateEntity("First") }, null);
            ViewModel.Tick(2f);
            ViewModel.Handle(new Died { Target = CreateEntity("Second") }, null);
            ViewModel.Tick(2.5f);
            Assert.AreEqual(2, ViewModel.Entries.Value.Count);

            ViewModel.Tick(0.5f);
            Assert.AreEqual(1, ViewModel.Entries.Value.Count);
            Assert.AreEqual("Second", ViewModel.Entries.Value[0].From);

            ViewModel.Tick(2f);
            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Tick_LargeDelta_ExpiresAllEntriesAndNotifiesBindings()
        {
            ViewModel.Handle(new Died { Target = CreateEntity("First") }, null);
            ViewModel.Handle(new Died { Target = CreateEntity("Second") }, null);
            int updates = 0;
            ViewModel.Entries.Bind(_ => updates++);

            ViewModel.Tick(10f);

            Assert.IsEmpty(ViewModel.Entries.Value);
            Assert.AreEqual(2, updates);
        }

        [Test]
        public void Tick_NonPositiveDelta_DoesNotAgeEntries()
        {
            ViewModel.Handle(new Died { Target = CreateEntity("Victim") }, null);
            ViewModel.Tick(-5f);
            ViewModel.Tick(0f);
            ViewModel.Tick(5f);

            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Handle_AssistedKill_SnapshotsHelperBeforeSkull()
        {
            IEntityBehaviour helper = CreateEntity("Helper", 3);
            ViewModel.Handle(new Died
            {
                From = CreateEntity("Killer", 1),
                Assistant = helper,
                Target = CreateEntity("Victim", 2)
            }, null);

            helper.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(3, "Changed", default));

            Assert.AreEqual("Killer + Helper", ViewModel.Entries.Value[0].From);
            Assert.AreEqual("Victim", ViewModel.Entries.Value[0].Target);
        }

        [TestCase("environment")]
        [TestCase("suicide")]
        [TestCase("killer")]
        [TestCase("victim")]
        public void Handle_IneligibleAssistant_PreservesExistingFormat(string reason)
        {
            IEntityBehaviour killer = CreateEntity("Killer", 1);
            IEntityBehaviour victim = CreateEntity("Victim", 2);
            IEntityBehaviour helper = CreateEntity("Helper", 3);

            switch (reason)
            {
                case "environment": killer = null; break;
                case "suicide": killer = victim; break;
                case "killer": helper = killer; break;
                case "victim": helper = victim; break;
            }

            ViewModel.Handle(new Died { From = killer, Assistant = helper, Target = victim }, null);

            Assert.AreEqual(killer is null || killer == victim ? "Victim" : "Killer",
                ViewModel.Entries.Value[0].From);
            Assert.AreEqual(killer is null ? null : "Victim", ViewModel.Entries.Value[0].Target);
        }

        private static IEntityBehaviour CreateEntity(string displayName = null, ulong playerId = 1)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            ITeamParticipantBehaviour participant = null;

            if (displayName != null)
            {
                participant = Substitute.For<ITeamParticipantBehaviour>();
                participant.State.Returns(new PlayerState(playerId, displayName, default));
            }

            entity.GetBehaviour<ITeamParticipantBehaviour>().Returns(participant);
            return entity;
        }
    }
}
