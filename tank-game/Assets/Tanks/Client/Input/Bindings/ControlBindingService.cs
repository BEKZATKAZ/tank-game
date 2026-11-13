using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tanks.Client.Input.Driving;
using Tanks.Core.Repositories;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Client.Input.Bindings
{
    public sealed partial class ControlBindingService :
        IControlBindingService, IDisposable
    {
        public ControlBindingService(IFileSystem files)
        {
            _store = new BindingStoreService(files, Path.Combine(
                Application.persistentDataPath, "bindings.json"));
        }

        private readonly BindingStoreService _store;
        private readonly HashSet<InputActionAsset> _assets = new();
        private readonly Dictionary<string, string> _defaults = new();
        private Dictionary<string, string> _committed;
        private Dictionary<string, string> _pending;
        private BindingCaptureService _capture;

        public event Action Changed;
        public event Action SuspensionChanged;

        public bool IsSuspended => _capture != null;
        public bool IsCapturing => _capture != null;
        public string CaptureSlot { get; private set; }
        public string Error { get; private set; }
        public bool IsDirty => _pending != null &&
            (_pending.Count != _committed.Count || _pending.Any(x =>
                _committed.TryGetValue(x.Key, out string value) == false ||
                value != x.Value));

        private void EnsureLoaded()
        {
            if (_committed != null) return;

            DirectionalButtonProcessor.Register();
            Controls defaults = new();
            foreach (KeyValuePair<string, string> slot in
                ControlBindingSlots.BindingIds)
            {
                InputBinding binding = defaults.asset.bindings.First(x =>
                    x.id.ToString() == slot.Value);

                _defaults[slot.Key] = binding.path;
            }

            InputAssetLifetime.Destroy(defaults.asset);
            _committed = _store.Load();
            _pending = new Dictionary<string, string>(_committed);
        }

        public void BeginEdit()
        {
            EnsureLoaded();
            Revert();
        }

        public string GetPath(string slot)
        {
            EnsureLoaded();
            return _pending.TryGetValue(slot, out string path)
                ? path : _defaults[slot];
        }

        public void SetPath(string slot, string path)
        {
            EnsureLoaded();
            if (ControlBindingSlots.BindingIds.ContainsKey(slot) == false) return;
            if (BindingPathUtility.IsValid(slot, path) == false) return;

            if (path == _defaults[slot]) _pending.Remove(slot);
            else _pending[slot] = path;

            Error = null;
            Changed?.Invoke();
        }

        public void Revert()
        {
            EnsureLoaded();
            CancelCapture();
            _pending = new Dictionary<string, string>(_committed);
            Error = null;
            Changed?.Invoke();
        }

        public bool Apply()
        {
            EnsureLoaded();
            CancelCapture();
            if (_store.Save(_pending, out string error) == false)
            {
                Error = error;
                Changed?.Invoke();
                return false;
            }

            _committed = new Dictionary<string, string>(_pending);
            foreach (InputActionAsset asset in _assets) Install(asset);

            Error = null;
            Changed?.Invoke();
            return true;
        }

        public bool Reset()
        {
            EnsureLoaded();
            _pending.Clear();
            return Apply();
        }

        public void Register(InputActionAsset asset)
        {
            EnsureLoaded();
            _assets.Add(asset);
            Install(asset);
        }

        public void Unregister(InputActionAsset asset) => _assets.Remove(asset);

        private void Install(InputActionAsset asset)
        {
            foreach (KeyValuePair<string, string> slot in
                ControlBindingSlots.BindingIds)
            {
                InputBinding mask = new() { id = Guid.Parse(slot.Value) };
                int index = asset.FindBinding(mask, out InputAction action);
                if (action == null) continue;

                string desired = _committed.GetValueOrDefault(slot.Key);
                if (action.bindings[index].overridePath == desired) continue;

                if (desired == null) action.RemoveBindingOverride(index);
                else action.ApplyBindingOverride(index, desired);
            }
        }

        public void Dispose() => CancelCapture();
    }
}
