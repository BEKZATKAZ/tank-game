using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using Tanks.Client.Input.Bindings;
using Tanks.Core.Settings;
using Tanks.UI.Menus.Sections;
using Tanks.UI.Menus.Settings;

namespace Tanks.UI.Tests.Menus
{
    public sealed class ControlSettingsTests : ViewModelTestBase<SettingsViewModel>
    {
        private IControlBindingService _bindings;
        private ISettingsService _settings;

        protected override SettingsViewModel CreateViewModel()
        {
            _bindings = Substitute.For<IControlBindingService>();
            _settings = Substitute.For<ISettingsService>();
            IMenuSectionService navigation = Substitute.For<IMenuSectionService>();

            _bindings.Apply().Returns(true);
            _bindings.Reset().Returns(true);

            return new SettingsViewModel(_settings, navigation, _bindings);
        }

        [TestCase("shoot")]
        [TestCase("modern.reverse")]
        public void ClearRowRemovesOnlyItsBindingsAndKeepsChangesPending(string row)
        {
            string[] slots = ControlBindingSlots.BindingIds.Keys
                .Where(x => x.StartsWith(row + ".", StringComparison.Ordinal))
                .ToArray();

            ViewModel.BeginEdit();
            _bindings.When(x => x.SetPath(Arg.Any<string>(), "")).Do(_ =>
            {
                _bindings.IsDirty.Returns(true);
                _bindings.Changed += Raise.Event<Action>();
            });

            ViewModel.ClearBindings(row);

            _bindings.Received().CancelCapture();
            foreach (string slot in slots) _bindings.Received(1).SetPath(slot, "");
            _bindings.Received(slots.Length).SetPath(Arg.Any<string>(), "");
            _bindings.DidNotReceive().Apply();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.True);

            ViewModel.RevertChanges();

            _bindings.Received().Revert();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.False);
        }
        [Test]
        public void PendingBindingsEnableActionsAndRevertClearsThem()
        {
            ViewModel.BeginEdit();
            _bindings.IsDirty.Returns(true);
            _bindings.Changed += Raise.Event<System.Action>();

            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.True);
            Assert.That(ViewModel.RevertButtonEnabled.Value, Is.True);

            ViewModel.RevertChanges();

            _bindings.Received().Revert();
            _settings.Received().RevertChanges();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.False);
        }

        [Test]
        public void FailedBindingSaveKeepsBothSetsOfEditsForRetry()
        {
            ViewModel.Write(PlayerSetting.ControlsMode, (int)ControlsMode.Classic);
            _bindings.Apply().Returns(false);

            ViewModel.ApplyChanges();

            _settings.DidNotReceive().ApplyChanges();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.True);
            _bindings.Apply().Returns(true);

            ViewModel.ApplyChanges();

            _settings.Received().ApplyChanges();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.False);
        }

        [Test]
        public void ResetResetsSettingsAndBindingsAndExitDiscardsPending()
        {
            ViewModel.Write(PlayerSetting.ControlsMode, (int)ControlsMode.Classic);
            ViewModel.ResetToDefaults();

            _bindings.Received().Reset();
            _settings.Received().ResetToDefaults();
            Assert.That(ViewModel.ApplyButtonEnabled.Value, Is.False);

            ViewModel.EndEdit();

            _bindings.Received().Revert();
            _settings.Received().RevertChanges();
        }
    }
}
