using System;
using Tanks.Client.Input.Bindings;
using Tanks.Client.Input.Driving;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input
{
    public abstract class InputServiceBase<T> : IInputService<T>, IDisposable
        where T : unmanaged
    {
        protected InputServiceBase(IControlBindingService bindings = null)
        {
            DirectionalButtonProcessor.Register();
            Controls = new Controls();
            _bindings = bindings;
            _bindings?.Register(Controls.asset);
            if (_bindings != null) _bindings.SuspensionChanged += RefreshMaps;
            Enable();
        }

        private readonly IControlBindingService _bindings;

        public bool IsEnabled { get; private set; }
        protected bool IsSuspended => _bindings?.IsSuspended == true;
        public InputScheme CurrentScheme { get; private set; }
        protected Controls Controls { get; }

        public abstract T Read();

        public virtual void Dispose()
        {
            Disable();
            if (_bindings != null) _bindings.SuspensionChanged -= RefreshMaps;
            _bindings?.Unregister(Controls.asset);
            InputAssetLifetime.Destroy(Controls.asset);
        }

        public void Enable()
        {
            if (IsEnabled) return;

            IsEnabled = true;
            RefreshMaps();

            InputSystem.onActionChange += OnActionChanged;
        }

        public void Disable()
        {
            if (IsEnabled == false) return;

            Controls.Disable();
            IsEnabled = false;

            InputSystem.onActionChange -= OnActionChanged;
        }

        protected void RefreshMaps()
        {
            Controls.Disable();
            if (IsEnabled && IsSuspended == false) EnableMaps();
        }

        protected virtual void EnableMaps() => Controls.Enable();

        private void OnActionChanged(object obj, InputActionChange change)
        {
            if (change is not InputActionChange.ActionPerformed) return;

            InputAction action = (InputAction)obj;

            if (action.actionMap?.asset != Controls.asset) return;
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
