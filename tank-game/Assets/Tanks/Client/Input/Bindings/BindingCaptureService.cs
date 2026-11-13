using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tanks.Client.Input.Bindings
{
    public sealed class BindingCaptureService : IDisposable
    {
        public BindingCaptureService(bool gamepad, Action<string> completed)
        {
            _gamepad = gamepad;
            _completed = completed;
        }

        private readonly bool _gamepad;
        private readonly Action<string> _completed;
        private bool _waitingForRelease = true;
        private bool _disposed;

        public void Start()
        {
            InputSystem.onAfterUpdate += Update;
            InputSystem.onDeviceChange += OnDeviceChanged;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            InputSystem.onAfterUpdate -= Update;
            InputSystem.onDeviceChange -= OnDeviceChanged;
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                _completed(null);
                return;
            }

            ButtonControl[] buttons = InputSystem.devices
                .SelectMany(x => x.allControls)
                .OfType<ButtonControl>().Where(x =>
                    BindingPathUtility.IsAssignable(x, _gamepad)).ToArray();

            if (_waitingForRelease)
            {
                bool held = InputSystem.devices
                .SelectMany(x => x.allControls)
                    .OfType<ButtonControl>().Any(x => x.isPressed);

                _waitingForRelease = held;
                return;
            }

            ButtonControl pressed = buttons.FirstOrDefault(x => x.isPressed);
            if (pressed == null) return;

            _completed(BindingPathUtility.CanonicalPath(pressed));
        }

        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change is InputDeviceChange.Disconnected or
                InputDeviceChange.Removed)
                _completed(null);
        }
    }
}
