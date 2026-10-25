using System.Linq;
using Tanks.UI.Menus.Settings.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings
{
    public sealed class SettingsView : ViewBase<SettingsViewModel>
    {
        private VisualElement _root;

        private Button _applyButton;
        private Button _revertButton;
        private Button _resetToDefaultsButton;

        private SettingElementBase[] _settings;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("SettingsMenu");
            _applyButton = _root.Q<Button>("ApplyButton");
            _revertButton = _root.Q<Button>("RevertButton");
            _resetToDefaultsButton = _root.Q<Button>("ResetToDefaultsButton");
            _settings = _root.Query<SettingElementBase>().Build().ToArray();
        }

        protected override void OnDestroy()
        {
            foreach (SettingElementBase setting in _settings) setting.OnDestroyed();
        }

        protected override void Bind(SettingsViewModel viewModel)
        {
            _applyButton.clicked += ViewModel.ApplyChanges;
            _revertButton.clicked += ViewModel.RevertChanges;
            _resetToDefaultsButton.clicked += ViewModel.ResetToDefaults;

            viewModel.ApplyButtonEnabled.Bind(x => _applyButton.enabledSelf = x);
            viewModel.RevertButtonEnabled.Bind(x => _revertButton.enabledSelf = x);

            foreach (SettingElementBase setting in _settings)
            {
                viewModel.SetBindTarget(setting.Type);
                viewModel.Bind(setting.SetValueWithoutNotify);

                setting.Previewed += viewModel.PreviewChanges;
                setting.ValueChanged += viewModel.Write;
            }
        }

        protected override void Unbind(SettingsViewModel viewModel)
        {
            viewModel.RevertChanges();

            _applyButton.clicked -= viewModel.ApplyChanges;
            _revertButton.clicked -= viewModel.RevertChanges;
            _resetToDefaultsButton.clicked -= viewModel.ResetToDefaults;

            foreach (SettingElementBase setting in _settings)
            {
                viewModel.SetBindTarget(setting.Type);
                viewModel.Unbind(setting.SetValueWithoutNotify);
                setting.ValueChanged -= viewModel.Write;
                setting.Previewed -= viewModel.PreviewChanges;
            }
        }
    }
}
