using Tanks.UI.Menus;
using Tanks.UI.Menus.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Pause
{
    public sealed class PauseMenuView : ViewBase<PauseMenuViewModel>
    {
        [SerializeField] private SettingsView _settings;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _buttons;
        private VisualElement _settingsContainer;
        private Button _resumeButton;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            root.AddManipulator(new MenuHighlightManipulator());
            _root = root;
            _root.pickingMode = PickingMode.Ignore;
            _overlay = root.Q("PauseOverlay");
            _buttons = root.Q("Buttons");
            _settingsContainer = root.Q("Menu");
            _resumeButton = root.Q<Button>("ResumeButton");

            _resumeButton.clicked += Resume;
            root.Q<Button>("SettingsButton").clicked += OpenSettings;
            root.Q<Button>("QuitButton").clicked += Quit;
        }

        protected override void Bind(PauseMenuViewModel viewModel)
        {
            viewModel.Section.Bind(OnSectionChanged);
            viewModel.IsQuitting.Bind(OnQuittingChanged);
        }

        protected override void Unbind(PauseMenuViewModel viewModel)
        {
            viewModel.Section.Unbind(OnSectionChanged);
            viewModel.IsQuitting.Unbind(OnQuittingChanged);
            _settings.Hide();
        }

        protected override void Update()
        {
            base.Update();
            ViewModel?.Update();
        }

        private void Resume() => ViewModel.Resume();
        private void OpenSettings() => ViewModel.OpenSettings();
        private void Quit() => ViewModel.ShowQuitConfirmation();

        private void OnQuittingChanged(bool quitting)
        {
            _buttons.SetEnabled(quitting == false);
            _settingsContainer.SetEnabled(quitting == false);
        }

        private void OnSectionChanged(PauseMenuSection section)
        {
            bool settingsOpen = section is PauseMenuSection.Settings;

            if (settingsOpen && _settings.IsVisible == false) _settings.Show();
            else if (settingsOpen == false) _settings.Hide();

            _settingsContainer.style.display = settingsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _overlay.style.display = section is PauseMenuSection.Closed
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (section is PauseMenuSection.Main) _resumeButton.Focus();
        }
    }
}
