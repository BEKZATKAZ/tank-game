using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input.Bindings
{
    public sealed partial class ControlBindingService
    {
        private readonly List<InputAction> _menuActions = new();

        public void Capture(string slot)
        {
            CancelCapture();
            if (ControlBindingSlots.BindingIds.ContainsKey(slot) == false) return;

            CaptureSlot = slot;
            _capture = new BindingCaptureService(
                slot.Contains(".gamepad."), FinishCapture);
            SuspensionChanged?.Invoke();

            foreach (InputAction action in InputSystem.ListEnabledActions())
            {
                _menuActions.Add(action);
                action.Disable();
            }

            _capture.Start();
            Changed?.Invoke();
        }

        public void CancelCapture()
        {
            if (_capture == null) return;

            _capture.Dispose();
            _capture = null;
            CaptureSlot = null;

            foreach (InputAction action in _menuActions) action.Enable();

            _menuActions.Clear();
            SuspensionChanged?.Invoke();
            Changed?.Invoke();
        }

        private void FinishCapture(string path)
        {
            string slot = CaptureSlot;

            CancelCapture();
            if (path != null) SetPath(slot, path);
        }
    }
}
