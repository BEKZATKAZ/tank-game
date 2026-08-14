using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainMenuSectionsView : ViewBase<MainMenuSectionsViewModel>
    {
        [SerializeField] private ViewBase _settingsView;

        private VisualElement _root;
        private ViewBase _currentSection;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Menu");

            Button hostLobbyButton = root.Q<Button>("HostLobbyButton");
            Button findLobbyButton = root.Q<Button>("JoinLobbyButton");
            Button settingsButton = root.Q<Button>("SettingsButton");
            Button quitButton = root.Q<Button>("QuitButton");

            hostLobbyButton.clicked += OpenLobby;
            findLobbyButton.clicked += OpenFindLobby;
            settingsButton.clicked += OpenSettings;
            quitButton.clicked += QuitGame;
        }

        protected override void Bind(MainMenuSectionsViewModel viewModel)
        {
            viewModel.Section.Bind(OnSectionOpened);
        }

        private void Start()
        {
            // Hide to prevent open menus on start
            _root.style.display = DisplayStyle.None;
        }

        private void OpenLobby() => ViewModel.OpenMenu(MenuType.Lobby);
        private void OpenFindLobby() => ViewModel.OpenSection(MainMenuSection.FindLobby);
        private void OpenSettings() => ViewModel.OpenSection(MainMenuSection.Settings);
        private void QuitGame() => Application.Quit();

        private void OnSectionOpened(MainMenuSection section)
        {
            ViewBase previousMenu = _currentSection;

            if (_currentSection != null) _currentSection.Hide();

            _currentSection = section switch
            {
                MainMenuSection.Settings => _settingsView,
                _ => null
            };

            // Flip flop effect
            if (previousMenu == _currentSection)
            {
                ViewModel.OpenSection(MainMenuSection.None);
                return;
            }

            if (_currentSection != null) _currentSection.Show();

            _root.style.display = _currentSection ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
