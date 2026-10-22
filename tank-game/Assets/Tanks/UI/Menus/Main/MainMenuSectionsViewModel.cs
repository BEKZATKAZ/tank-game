using Cysharp.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.Shared.Playground;
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
            IMenuNavigation navigation,
            ISceneLoader sceneLoader,
            IRunServerUseCase runServer)
        {
            _auth = auth;
            _lobbyService = lobbyService;
            _localization = localization;
            _loading = loading;
            _dialog = dialog;
            _navigation = navigation;
            _sceneLoader = sceneLoader;
            _runServer = runServer;
        }

        public BindableProperty<MainMenuSection> Section { get; } = new();

        private readonly IAuthService _auth;
        private readonly ILobbyService _lobbyService;
        private readonly ILocalizationDatabase _localization;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;
        private readonly ISceneLoader _sceneLoader;
        private readonly IRunServerUseCase _runServer;

        public void OpenSection(MainMenuSection section)
        {
            Section.Value = section;
        }

        public void ShowQuitGameConfirmation()
        {
            _dialog.ShowConfirmation("quit", onYes: Application.Quit);
        }

        public async UniTaskVoid OpenTutorial()
        {
            _localization.TryGet("Menus", "menu.lobby.starting-server", out var starting);
            _loading.Begin(starting);

            ResultType started = await _runServer.RunAsync(
                RequestedServerScene.Playground, authorize: false);

            if (started.IsSuccess == false)
            {
                _dialog.ShowErrorFromCode("error.lobby", started.ErrorCode);
                _loading.End();
                return;
            }

            _sceneLoader.StartLoading(new LoadPlayground());
            _loading.End();
        }

        public async UniTaskVoid CreateLobby()
        {
            string lobbyName = "My lobby";

            if (_localization.TryGet(
                "Menus", "menu.lobby.default-name", out var defaultName))
            {
                defaultName.AddVariable("name", _auth.CurrentProfile.DisplayName.Value);
                lobbyName = defaultName.Get();
            }

            _localization.TryGet("Menus", "menu.lobby.starting-server", out var starting);
            _loading.Begin(starting);

            ResultType started = await _runServer.RunAsync();

            if (started.IsSuccess == false)
            {
                _dialog.ShowErrorFromCode("error.lobby", started.ErrorCode);
                _loading.End();
                return;
            }

            _localization.TryGet("Menus", "menu.lobby.creating", out var creating);
            _loading.Begin(creating);

            ResultType result = await _lobbyService.HostLobbyAsync(lobbyName, 4);
            _loading.End();

            if (result.IsSuccess) _navigation.Navigate(MenuType.Lobby);
            else _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
        }
    }
}
