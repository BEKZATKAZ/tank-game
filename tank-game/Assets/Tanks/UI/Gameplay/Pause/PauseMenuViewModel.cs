using Cysharp.Threading.Tasks;
using System;
using Tanks.Client.Input;
using Tanks.Core;
using Tanks.Core.Common.Scenes;
using Tanks.UI.Gameplay.Input;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.Pause
{
    public enum PauseMenuSection
    {
        Closed,
        Main,
        Settings
    }

    public sealed class PauseMenuViewModel : IViewModel, IDisposable
    {
        public PauseMenuViewModel(
            IInputService<ManualDriveInput> drive,
            IInputService<TurretInput> turret,
            IInputService<UIGameplayInput> input,
            IGameplayExitService exit,
            IDialogScreenService dialog)
        {
            _drive = drive;
            _turret = turret;
            _input = input;
            _exit = exit;
            _dialog = dialog;
        }

        public BindableProperty<PauseMenuSection> Section { get; } = new();
        public BindableProperty<bool> IsQuitting { get; } = new();
        public bool IsOpen => Section.Value is not PauseMenuSection.Closed;

        private readonly IInputService<ManualDriveInput> _drive;
        private readonly IInputService<TurretInput> _turret;
        private readonly IInputService<UIGameplayInput> _input;
        private readonly IGameplayExitService _exit;
        private readonly IDialogScreenService _dialog;
        private bool _driveWasEnabled;
        private bool _turretWasEnabled;
        private bool _confirming;
        private bool _disposed;

        public void Update()
        {
            if (_input.Read().Pause) Toggle();
        }

        public void Toggle()
        {
            if (CanNavigate() == false) return;

            if (Section.Value is PauseMenuSection.Settings)
            {
                Section.Value = PauseMenuSection.Main;
                return;
            }

            if (IsOpen) Resume();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || CanNavigate() == false) return;

            _driveWasEnabled = _drive.IsEnabled;
            _turretWasEnabled = _turret.IsEnabled;
            _drive.Disable();
            _turret.Disable();

            Section.Value = PauseMenuSection.Main;
        }

        public void OpenSettings()
        {
            if (IsOpen && CanNavigate()) Section.Value = PauseMenuSection.Settings;
        }

        public void Resume()
        {
            if (IsOpen == false || CanNavigate() == false) return;

            Section.Value = PauseMenuSection.Closed;
            RestoreInput();
        }

        public void ShowQuitConfirmation()
        {
            if (IsOpen == false || CanNavigate() == false) return;
            _confirming = true;

            _dialog.ShowConfirmation("quit-to-menu",
                onYes: () => QuitAsync().Forget(),
                onNo: () => _confirming = false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (IsOpen) RestoreInput();
        }

        private bool CanNavigate()
        {
            return _disposed == false && _confirming == false &&
                IsQuitting.Value == false && _dialog.IsVisible == false;
        }

        private void RestoreInput()
        {
            if (_driveWasEnabled) _drive.Enable();
            if (_turretWasEnabled) _turret.Enable();
        }

        private async UniTask QuitAsync()
        {
            if (_disposed || _confirming == false || IsQuitting.Value) return;
            _confirming = false;
            IsQuitting.Value = true;

            ResultType result;

            try
            {
                result = await _exit.ExitAsync();
            }
            catch (Exception)
            {
                result = Result.Failure("failed-to-leave");
            }

            if (_disposed || result.IsSuccess) return;

            IsQuitting.Value = false;
            _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
        }
    }
}
