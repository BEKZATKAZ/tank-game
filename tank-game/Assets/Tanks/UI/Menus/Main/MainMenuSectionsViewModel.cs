using Cysharp.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainMenuSectionsViewModel : IViewModel
    {
        public MainMenuSectionsViewModel(
            IAuthService auth,
            ILobbyService lobbyService,
            ILocalizationDatabase localization,
            ILoadingScreenService loading,
            IDialogScreenService dialog,
            IMenuNavigation navigation)
        {
            _auth = auth;
            _lobbyService = lobbyService;
            _localization = localization;
            _loading = loading;
            _dialog = dialog;
            _navigation = navigation;
        }

        public BindableProperty<MainMenuSection> Section { get; } = new();

        private readonly IAuthService _auth;
        private readonly ILobbyService _lobbyService;
        private readonly ILocalizationDatabase _localization;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;

        public void OpenSection(MainMenuSection section)
        {
            Section.Value = section;
        }

        public void ShowQuitGameConfirmation()
        {
            _dialog.ShowConfirmation("quit");
        }

        public async UniTaskVoid CreateLobby()
        {
            string lobbyName = "My lobby";

            if (_localization.TryGet(
                "Menus", "menu.lobby.default-name", out ILocalizedString defaultName))
            {
                defaultName.AddVariable("name", _auth.CurrentProfile.DisplayName.Value);
                lobbyName = defaultName.Get();
            }

            _localization.TryGet("Menus", "menu.lobby.creating", out ILocalizedString creating);
            _loading.Begin(creating);

            ResultType result = await _lobbyService.HostLobbyAsync(lobbyName, 4);
            _loading.End();

            if (result.IsSuccess) _navigation.Navigate(MenuType.Lobby);
            else _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
        }
    }
}
