using System;
using Tanks.Client.Input.Bindings;
using Tanks.Utils;

namespace Tanks.UI.Menus.Settings
{
    public sealed class BindingCaptureDialogViewModel : IDisposable
    {
        public BindingCaptureDialogViewModel(IControlBindingService bindings)
        {
            _bindings = bindings;
        }

        private readonly IControlBindingService _bindings;
        public BindableProperty<bool> IsVisible { get; } = new();
        private bool _initialized;

        public bool IsGamepad =>
            _bindings.CaptureSlot?.Contains(".gamepad.") == true;

        public void Initialize()
        {
            if (_initialized) return;

            _initialized = true;
            _bindings.Changed += Refresh;
            Refresh();
        }

        public void Capture(string slot) => _bindings.Capture(slot);
        public void Cancel() => _bindings.CancelCapture();

        public void Dispose()
        {
            _bindings.Changed -= Refresh;
            _initialized = false;
            _bindings.CancelCapture();
            IsVisible.Value = false;
        }

        private void Refresh() => IsVisible.Value = _bindings.IsCapturing;
    }
}
