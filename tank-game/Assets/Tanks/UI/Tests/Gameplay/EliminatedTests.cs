using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Tanks.Localization;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.UI.Gameplay.Eliminated;

namespace Tanks.UI.Tests.Gameplay
{
    public class EliminatedTests : ViewModelTestBase<EliminatedViewModel>
    {
        private IMessageBus _bus;
        private ILocalizationDatabase _localization;
        private IEntityBehaviour _target;

        protected override EliminatedViewModel CreateViewModel()
        {
            _bus = Substitute.For<IMessageBus>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _target = Substitute.For<IEntityBehaviour>();
            _target.IsPossessed.Returns(true);
            return new EliminatedViewModel(_bus, _localization);
        }

        [Test]
        public void Constructor_RegistersDeathHandler()
        {
            _bus.Received(1).RegisterHandler<Died>(ViewModel);
        }

        [Test]
        public void Dispose_UnregistersDeathHandler()
        {
            var viewModel = new EliminatedViewModel(_bus, _localization);
            viewModel.Dispose();
            _bus.Received(1).UnregisterHandler<Died>(viewModel);
        }

        [Test]
        public void Handle_UnpossessedTarget_PreservesLabelWithoutLocalization()
        {
            _target.IsPossessed.Returns(false);
            ViewModel.EliminatedBy.Value = "Previous";

            ViewModel.Handle(new Died { Target = _target, From = _target }, null);

            Assert.AreEqual("Previous", ViewModel.EliminatedBy.Value);
            _localization.DidNotReceiveWithAnyArgs().TryGet(default, default, out _);
        }

        [Test]
        public void Handle_SelfKill_UsesSelfLocalization()
        {
            ILocalizedString text = MockEntry("gameplay.eliminated-by-self", "Self elimination");

            ViewModel.Handle(new Died { Target = _target, From = _target }, null);

            Assert.AreEqual("Self elimination", ViewModel.EliminatedBy.Value);
            text.Received(1).Get();
            _localization.DidNotReceive().TryGet("Gameplay", "gameplay.eliminated-by", out _);
        }

        [Test]
        public void Handle_PlayerKill_PassesKillerNameBeforeResolvingText()
        {
            IEntityBehaviour from = CreateKiller();
            ILocalizedString text = MockEntry("gameplay.eliminated-by", "Eliminated by Killer");

            ViewModel.Handle(new Died { Target = _target, From = from }, null);

            Assert.AreEqual("Eliminated by Killer", ViewModel.EliminatedBy.Value);
            Received.InOrder(() =>
            {
                text.AddVariable("name", "Killer");
                text.Get();
            });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Handle_NoPlayerSource_ClearsPreviousLabel(bool missingSource)
        {
            IEntityBehaviour from = null;
            if (missingSource == false)
            {
                from = Substitute.For<IEntityBehaviour>();
                from.GetBehaviour<ITeamParticipantBehaviour>().Returns((ITeamParticipantBehaviour)null);
            }

            ViewModel.EliminatedBy.Value = "Previous";
            ViewModel.Handle(new Died { Target = _target, From = from }, null);

            Assert.AreEqual("", ViewModel.EliminatedBy.Value);
            _localization.DidNotReceiveWithAnyArgs().TryGet(default, default, out _);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Handle_MissingLocalization_UsesExpectedFallback(bool selfKill)
        {
            _localization.TryGet("Gameplay", Arg.Any<string>(), out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    args[2] = null;
                    return false;
                });
            ViewModel.EliminatedBy.Value = "Previous";

            ViewModel.Handle(new Died
            {
                Target = _target,
                From = selfKill ? _target : CreateKiller()
            }, null);

            Assert.AreEqual(selfKill ? null : "", ViewModel.EliminatedBy.Value);
        }

        [Test]
        public void Handle_LocalizationReturnsNull_UsesEmptyLabel()
        {
            MockEntry("gameplay.eliminated-by", null);
            ViewModel.Handle(new Died { Target = _target, From = CreateKiller() }, null);
            Assert.AreEqual("", ViewModel.EliminatedBy.Value);
        }

        [Test]
        public void Handle_RepeatedElimination_NotifiesBindingsAgain()
        {
            MockEntry("gameplay.eliminated-by-self", "Self elimination");
            Died message = new Died { Target = _target, From = _target };
            ViewModel.Handle(message, null);
            List<string> updates = new List<string>();
            ViewModel.EliminatedBy.Bind(updates.Add);
            updates.Clear();

            ViewModel.Handle(message, null);

            CollectionAssert.AreEqual(new string[] { null, "Self elimination" }, updates);
        }

        private static IEntityBehaviour CreateKiller()
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            ITeamParticipantBehaviour participant = Substitute.For<ITeamParticipantBehaviour>();
            participant.State.Returns(new PlayerState(1, "Killer", default));
            entity.GetBehaviour<ITeamParticipantBehaviour>().Returns(participant);
            return entity;
        }

        private ILocalizedString MockEntry(string key, string value)
        {
            ILocalizedString text = Substitute.For<ILocalizedString>();
            text.Get().Returns(value);
            _localization.TryGet("Gameplay", key, out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    args[2] = text;
                    return true;
                });
            return text;
        }
    }
}
