using System;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input.Bindings
{
    public interface IControlBindingService
    {
        event Action Changed;
        event Action SuspensionChanged;
        bool IsDirty { get; }
        bool IsSuspended { get; }
        bool IsCapturing { get; }
        string CaptureSlot { get; }
        string Error { get; }

        string GetPath(string slot);
        void SetPath(string slot, string path);
        void BeginEdit();
        void Revert();
        bool Apply();
        bool Reset();
        void Register(InputActionAsset asset);
        void Unregister(InputActionAsset asset);
        void Capture(string slot);
        void CancelCapture();
    }
}
