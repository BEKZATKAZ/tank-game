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
using Tanks.UI.Menus.Sections;
using Tanks.UI.TankModel;

namespace Tanks.UI.Menus.Play
{
    public sealed class PlayViewModel : IViewModel
    {
        public PlayViewModel(
            IAuthService auth,
            IRunServerUseCase runServer,
            ISceneLoader sceneLoader,
            ILobbyService lobbies,
            IMenuSectionService sectionService,
            ILoadingScreenService loading,
            IDialogScreenService dialog,
            ITankModelService tankModelService,
            ILocalizationDatabase localization)
        {
            _auth = auth;
            _runServer = runServer;
            _sceneLoader = sceneLoader;
            _lobbies = lobbies;
            _navigation = sectionService;
            _loading = loading;
            _dialog = dialog;
            _tankModelService = tankModelService;
            _localization = localization;
        }

        private readonly IAuthService _auth;
        private readonly IRunServerUseCase _runServer;
        private readonly ISceneLoader _sceneLoader;
        private readonly ILobbyService _lobbies;
        private readonly IMenuSectionService _navigation;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly ITankModelService _tankModelService;
        private readonly ILocalizationDatabase _localization;

        public void Back() => _navigation.OpenSection(MenuSectionType.Main);

        public async UniTask OpenTutorialAsync()
        {
            _navigation.OpenSection(MenuSectionType.None, instant: true);
            _tankModelService.StartDrivingInPlace();

            _localization.TryGet("Menus", "menu.lobby.starting-server", out var starting);
            _loading.Begin(starting);

            ResultType started = await _runServer.RunAsync(
                RequestedServerScene.Playground, authorize: false);

            if (started.IsSuccess)
            {
                _sceneLoader.StartLoading(new LoadPlayground());
                return;
            }

            _dialog.ShowErrorFromCode("error.lobby", started.ErrorCode);
            _loading.End();

            _tankModelService.StopDrivingInPlace();
            _navigation.OpenSection(MenuSectionType.Play);
        }

        public async UniTask CreateLobbyAsync()
        {
            _navigation.OpenSection(MenuSectionType.None, instant: true);
            _tankModelService.StartDrivingInPlace();

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
                _tankModelService.StopDrivingInPlace();
                _loading.End();
                _navigation.OpenSection(MenuSectionType.Play);
                return;
            }

            _localization.TryGet("Menus", "menu.lobby.creating", out var creating);
            _loading.Begin(creating);

            ResultType result = await _lobbies.HostLobbyAsync(lobbyName, 8);

            _loading.End();
            _tankModelService.StopDrivingInPlace();

            if (result.IsSuccess)
            {
                _tankModelService.DriveByCurve(0, 0, instant: false);
                _navigation.OpenSection(MenuSectionType.LobbyRoom, instant: true, menuDelay: 2.5f);
                return;
            }

            _dialog.ShowErrorFromCode("error.lobby", started.ErrorCode);
            _navigation.OpenSection(MenuSectionType.Play);
        }

        public void FindLobby()
        {
            _navigation.OpenSection(MenuSectionType.LobbyBrowser, instant: false);
        }
    }
}
