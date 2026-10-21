using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainMenuSectionsView : ViewBase<MainMenuSectionsViewModel>
    {
        [SerializeField] private ViewBase _findLobbyView;
        [SerializeField] private ViewBase _settingsView;

        private VisualElement _root;
        private VisualElement _menuContainer;
        private ViewBase _currentSection;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _menuContainer = root.Q("Menu");

            Button hostLobbyButton = root.Q<Button>("HostLobbyButton");
            Button findLobbyButton = root.Q<Button>("JoinLobbyButton");
            Button tutorialButton = root.Q<Button>("TutorialButton");
            Button settingsButton = root.Q<Button>("SettingsButton");
            Button quitButton = root.Q<Button>("QuitButton");

            hostLobbyButton.clicked += OpenLobby;
            findLobbyButton.clicked += OpenFindLobby;
            tutorialButton.clicked += OpenTutorial;
            settingsButton.clicked += OpenSettings;
            quitButton.clicked += QuitGame;
        }

        protected override void Bind(MainMenuSectionsViewModel viewModel)
        {
            _menuContainer.style.display = DisplayStyle.None;
            _settingsView.Hide();
            _findLobbyView.Hide();
            viewModel.Section.Bind(OnSectionOpened);
        }

        private void OpenLobby() => ViewModel.CreateLobby().Forget();
        private void OpenFindLobby() => ViewModel.OpenSection(MainMenuSection.FindLobby);
        private void OpenTutorial() => ViewModel.OpenTutorial().Forget();
        private void OpenSettings() => ViewModel.OpenSection(MainMenuSection.Settings);
        private void QuitGame() => ViewModel.ShowQuitGameConfirmation();

        private void OnSectionOpened(MainMenuSection section)
        {
            ViewBase previousMenu = _currentSection;

            if (_currentSection) _currentSection.Hide();

            _currentSection = section switch
            {
                MainMenuSection.FindLobby => _findLobbyView,
                MainMenuSection.Settings => _settingsView,
                _ => null
            };

            // Flip flop effect
            // Check not null to prevent stack overflows (null == null is true)
            if (previousMenu && previousMenu == _currentSection)
            {
                ViewModel.OpenSection(MainMenuSection.None);
                return;
            }

            if (_currentSection) _currentSection.Show();

            _menuContainer.style.display = _currentSection ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
