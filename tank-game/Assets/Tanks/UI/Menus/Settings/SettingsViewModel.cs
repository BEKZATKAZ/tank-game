using System;
using Tanks.Core.Settings;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.Settings
{
    public sealed class SettingsViewModel : IViewModel
    {
        public SettingsViewModel(ISettingsService service, IMenuSectionService navigation)
        {
            _service = service;
            _navigation = navigation;
        }

        public BindableProperty<bool> ApplyButtonEnabled { get; } = new();
        public BindableProperty<bool> RevertButtonEnabled { get; } = new();

        private readonly ISettingsService _service;
        private readonly IMenuSectionService _navigation;
        private PlayerSetting _targetSetting;

        public void Back()
        {
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

            ApplyButtonEnabled.Value = true;
            RevertButtonEnabled.Value = true;
        }

        public void ResetToDefaults()
        {
            _service.ResetToDefaults();
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }

        public void RevertChanges()
        {
            _service.RevertChanges();
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }

        public void ApplyChanges()
        {
            _service.ApplyChanges();
            ApplyButtonEnabled.Value = false;
            RevertButtonEnabled.Value = false;
        }
    }
}
