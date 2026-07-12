using System;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input
{
    public abstract class InputServiceBase<T> : IInputService<T>, IDisposable where T : unmanaged
    {
        protected InputServiceBase()
        {
            Controls = new Controls();
            Enable();
        }

        public bool IsEnabled { get; private set; }
        public InputScheme CurrentScheme { get; private set; }
        protected Controls Controls { get; }

        public abstract T Read();

        public void Dispose()
        {
            Disable();
            Controls.Dispose();
        }

        public void Enable()
        {
            if (IsEnabled) return;
            Controls.Enable();
            InputSystem.onActionChange += OnActionChanged;
        }

        public void Disable()
        {
            if (IsEnabled == false) return;
            Controls.Disable();
            InputSystem.onActionChange -= OnActionChanged;
        }

        private void OnActionChanged(object obj, InputActionChange change)
        {
            if (change is not InputActionChange.ActionStarted) return;

            InputAction action = (InputAction)obj;

            if (action.activeControl is null) return;

            CurrentScheme = action.activeControl.device switch
            {
                Keyboard or Mouse => InputScheme.KeyboardAndMouse,
                Gamepad => InputScheme.Gamepad,
                _ => InputScheme.KeyboardAndMouse
            };
        }
    }
}
