using System;
using System.Linq;
using Tanks.Client.Input.Bindings;
using Tanks.Core.Settings;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.Settings
{
    public sealed class SettingsViewModel : IViewModel, IDisposable
    {
        public SettingsViewModel(
            ISettingsService service,
            IMenuSectionService navigation,
            IControlBindingService bindings)
        {
            _service = service;
            _navigation = navigation;
            _bindings = bindings;
            CaptureDialog = new BindingCaptureDialogViewModel(bindings);
        }

        public BindableProperty<bool> ApplyButtonEnabled { get; } = new();
        public BindableProperty<bool> RevertButtonEnabled { get; } = new();
        public BindingCaptureDialogViewModel CaptureDialog { get; }

        private readonly ISettingsService _service;
        private readonly IMenuSectionService _navigation;
        private readonly IControlBindingService _bindings;
        private PlayerSetting _targetSetting;
        private bool _settingsDirty;

        public IControlBindingService Bindings => _bindings;

        public void BeginEdit()
        {
            _bindings.BeginEdit();
            CaptureDialog.Initialize();
            _bindings.Changed += RefreshButtons;
            RefreshButtons();
        }

        public void EndEdit()
        {
            CaptureDialog.Dispose();
            _bindings.Changed -= RefreshButtons;
            RevertChanges();
        }

        public void Dispose() => EndEdit();

        public void ClearBindings(string row)
        {
            CaptureDialog.Cancel();

            foreach (string slot in ControlBindingSlots.BindingIds.Keys
                .Where(x => x.StartsWith(row + ".", StringComparison.Ordinal)))
            {
                _bindings.SetPath(slot, "");
            }
        }

        private void RefreshButtons()
        {
            bool dirty = _settingsDirty || _bindings.IsDirty;

            ApplyButtonEnabled.Value = dirty;
            RevertButtonEnabled.Value = dirty;
        }

        public void Back()
        {
            RevertChanges();
            _navigation.OpenSection(MenuSectionType.Main, instant: false);
        }

        public void SetBindTarget(PlayerSetting targetSetting)
        {
            _targetSetting = targetSetting;
        }

        public void PreviewChanges(PlayerSetting setting, int newValue)
        {
            _service.PreviewChanges(setting, newValue);
        }

        public void Bind(Action<int> callback)
        {
            _service.Bind(_targetSetting, callback);
        }

        public void Unbind(Action<int> callback)
        {
            _service.Unbind(_targetSetting, callback);
        }

        public void Write(PlayerSetting type, int value)
        {
            _service.Write(type, value);

            _settingsDirty = true;
            RefreshButtons();
        }

        public void ResetToDefaults()
        {
            if (_bindings.Reset() == false) return;

            _service.ResetToDefaults();
            _settingsDirty = false;
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }

        public void RevertChanges()
        {
            _bindings.Revert();
            _service.RevertChanges();
            _settingsDirty = false;
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }

        public void ApplyChanges()
        {
            if (_bindings.Apply() == false) return;

            _service.ApplyChanges();
            _settingsDirty = false;
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }
    }
}
