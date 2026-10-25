using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Client.Input;
using Tanks.Core;
using Tanks.Core.Common.Scenes;
using Tanks.UI.Gameplay.Input;
using Tanks.UI.Gameplay.Pause;
using Tanks.UI.Menus.Dialogs;

namespace Tanks.UI.Tests.Gameplay
{
    public class PauseMenuTests : ViewModelTestBase<PauseMenuViewModel>
    {
        private IInputService<ManualDriveInput> _drive;
        private IInputService<TurretInput> _turret;
        private IInputService<UIGameplayInput> _input;
        private IGameplayExitService _exit;
        private IDialogScreenService _dialog;
        private Action _yes;
        private Action _no;

        protected override PauseMenuViewModel CreateViewModel()
        {
            _drive = Substitute.For<IInputService<ManualDriveInput>>();
            _turret = Substitute.For<IInputService<TurretInput>>();
            _input = Substitute.For<IInputService<UIGameplayInput>>();
            _exit = Substitute.For<IGameplayExitService>();
            _dialog = Substitute.For<IDialogScreenService>();

            _drive.IsEnabled.Returns(true);
            _turret.IsEnabled.Returns(true);
            _exit.ExitAsync().Returns(UniTask.FromResult(Result.Success()));
            _dialog.When(x => x.ShowConfirmation("quit-to-menu", Arg.Any<Action>(), Arg.Any<Action>()))
                .Do(args =>
                {
                    _yes = args.ArgAt<Action>(1);
                    _no = args.ArgAt<Action>(2);
                });

            return new PauseMenuViewModel(_drive, _turret, _input, _exit, _dialog);
        }

        [Test]
        public void Create_StartsClosedWithoutChangingInput()
        {
            Assert.That(ViewModel.IsOpen, Is.False);
            Assert.That(ViewModel.Section.Value, Is.EqualTo(PauseMenuSection.Closed));
            _drive.DidNotReceive().Disable();
            _turret.DidNotReceive().Disable();
        }

        [Test]
        public void Update_PausePressed_TogglesMenu()
        {
            _input.Read().Returns(new UIGameplayInput { Pause = true });

            ViewModel.Update();

            Assert.That(ViewModel.Section.Value, Is.EqualTo(PauseMenuSection.Main));
            _drive.Received(1).Disable();
            _turret.Received(1).Disable();

            ViewModel.Update();

            Assert.That(ViewModel.IsOpen, Is.False);
            _drive.Received(1).Enable();
            _turret.Received(1).Enable();
        }

        [Test]
        public void Update_PauseNotPressed_KeepsMenuClosed()
        {
            ViewModel.Update();

            Assert.That(ViewModel.IsOpen, Is.False);
            _drive.DidNotReceive().Disable();
        }

        [Test]
        public void Toggle_SettingsOpen_ReturnsToPauseWithoutRestoringInput()
        {
            ViewModel.Open();
            ViewModel.OpenSettings();

            Assert.That(ViewModel.Section.Value, Is.EqualTo(PauseMenuSection.Settings));

            ViewModel.Toggle();

            Assert.That(ViewModel.Section.Value, Is.EqualTo(PauseMenuSection.Main));
            _drive.DidNotReceive().Enable();
            _turret.DidNotReceive().Enable();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Resume_RestoresOnlyPreviouslyEnabledInputs(bool driveEnabled, bool turretEnabled)
        {
            _drive.IsEnabled.Returns(driveEnabled);
            _turret.IsEnabled.Returns(turretEnabled);
            ViewModel.Open();
            ViewModel.Open();

            ViewModel.Resume();
            ViewModel.Resume();

            _drive.Received(1).Disable();
            _turret.Received(1).Disable();
            _drive.Received(driveEnabled ? 1 : 0).Enable();
            _turret.Received(turretEnabled ? 1 : 0).Enable();
            Assert.That(ViewModel.IsOpen, Is.False);
        }

        [Test]
        public void Dispose_SettingsOpen_RestoresInputOnceAndIgnoresFurtherActions()
        {
            ViewModel.Open();
            ViewModel.OpenSettings();

            ViewModel.Dispose();
            ViewModel.Dispose();
            ViewModel.Toggle();

            _drive.Received(1).Enable();
            _turret.Received(1).Enable();
        }

        [Test]
        public void OpenSettings_MenuClosed_DoesNothing()
        {
            ViewModel.OpenSettings();

            Assert.That(ViewModel.IsOpen, Is.False);
        }

        [Test]
        public void ShowQuitConfirmation_Cancelled_KeepsControlsBlockedAndAllowsResume()
        {
            ViewModel.Open();
            ViewModel.ShowQuitConfirmation();

            ViewModel.ShowQuitConfirmation();
            ViewModel.Resume();
            ViewModel.Toggle();

            _dialog.Received(1).ShowConfirmation("quit-to-menu", Arg.Any<Action>(), Arg.Any<Action>());
            _exit.DidNotReceive().ExitAsync();
            Assert.That(ViewModel.IsOpen, Is.True);
            _drive.DidNotReceive().Enable();

            _no();
            ViewModel.Resume();

            Assert.That(ViewModel.IsOpen, Is.False);
        }

        [Test]
        public void Quit_Confirmed_ExitsOnceAndKeepsControlsBlocked()
        {
            UniTaskCompletionSource<ResultType> completion = new();
            _exit.ExitAsync().Returns(completion.Task);
            ViewModel.Open();
            ViewModel.ShowQuitConfirmation();

            _yes();
            _yes();
            ViewModel.ShowQuitConfirmation();
            ViewModel.OpenSettings();
            ViewModel.Resume();

            _exit.Received(1).ExitAsync();
            Assert.That(ViewModel.IsQuitting.Value, Is.True);
            Assert.That(ViewModel.Section.Value, Is.EqualTo(PauseMenuSection.Main));
            _drive.DidNotReceive().Enable();

            completion.TrySetResult(Result.Success());
            ViewModel.Toggle();

            Assert.That(ViewModel.IsQuitting.Value, Is.True);
            _exit.Received(1).ExitAsync();
            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
        }

        [Test]
        public void Quit_Failed_ShowsErrorAndAllowsRetry()
        {
            _exit.ExitAsync().Returns(UniTask.FromResult(Result.Failure("failed-to-leave")));
            ViewModel.Open();
            ViewModel.ShowQuitConfirmation();

            _yes();

            Assert.That(ViewModel.IsQuitting.Value, Is.False);
            Assert.That(ViewModel.IsOpen, Is.True);
            _drive.DidNotReceive().Enable();
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "failed-to-leave");

            _exit.ExitAsync().Returns(UniTask.FromResult(Result.Success()));
            ViewModel.ShowQuitConfirmation();
            _yes();

            _exit.Received(2).ExitAsync();
            Assert.That(ViewModel.IsQuitting.Value, Is.True);
        }

        [Test]
        public void Quit_Throws_ShowsErrorAndKeepsMenuOpen()
        {
            _exit.ExitAsync().Returns(UniTask.FromException<ResultType>(new InvalidOperationException()));
            ViewModel.Open();
            ViewModel.ShowQuitConfirmation();

            _yes();

            Assert.That(ViewModel.IsQuitting.Value, Is.False);
            Assert.That(ViewModel.IsOpen, Is.True);
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "failed-to-leave");
        }

        [Test]
        public void Toggle_DialogVisible_KeepsMenuOpen()
        {
            ViewModel.Open();
            _dialog.IsVisible.Returns(true);

            ViewModel.Toggle();
            ViewModel.Resume();

            Assert.That(ViewModel.IsOpen, Is.True);
            _drive.DidNotReceive().Enable();
        }

        [Test]
        public void Quit_CompletesAfterDisposal_DoesNotShowError()
        {
            UniTaskCompletionSource<ResultType> completion = new();
            _exit.ExitAsync().Returns(completion.Task);
            ViewModel.Open();
            ViewModel.ShowQuitConfirmation();
            _yes();

            ViewModel.Dispose();
            completion.TrySetResult(Result.Failure("failed-to-leave"));

            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
            _drive.Received(1).Enable();
        }
    }
}
