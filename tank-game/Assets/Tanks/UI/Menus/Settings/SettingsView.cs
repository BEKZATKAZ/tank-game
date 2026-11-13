using System.Linq;
using Tanks.UI.Menus.Settings.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings
{
    public sealed class SettingsView : ViewBase<SettingsViewModel>
    {
        private VisualElement _root;
        private ControlMappingsView _mappings;

        private Button _applyButton;
        private Button _revertButton;
        private Button _resetToDefaultsButton;

        private SettingElementBase[] _settings;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            VisualElement section = root.Q("SettingsSection");
            _root = section.parent is TemplateContainer template ? template : section;
            section.style.display = DisplayStyle.Flex;
            _root.style.display = DisplayStyle.None;
            _root.AddManipulator(new MenuHighlightManipulator());
            _mappings = new ControlMappingsView();
            _mappings.Initialize(_root);

            _applyButton = _root.Q<Button>("ApplyButton");
            _revertButton = _root.Q<Button>("RevertButton");
            _resetToDefaultsButton = _root.Q<Button>("ResetToDefaultsButton");

            _applyButton.clicked += () => ViewModel.ApplyChanges();
            _revertButton.clicked += () => ViewModel.RevertChanges();
            _resetToDefaultsButton.clicked += () => ViewModel.ResetToDefaults();

            _root.Q<Button>("BackButton").clicked += () => ViewModel.Back();
            _settings = _root.Query<SettingElementBase>().Build().ToArray();
        }

        protected override void OnDestroy()
        {
            foreach (SettingElementBase setting in _settings) setting.OnDestroyed();
        }

        protected override void Bind(SettingsViewModel viewModel)
        {
            viewModel.BeginEdit();
            _mappings.Bind(
                viewModel.Bindings, viewModel.CaptureDialog, viewModel.ClearBindings);

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
            _mappings.Unbind();
            viewModel.EndEdit();

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
