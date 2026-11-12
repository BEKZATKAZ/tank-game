using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Sections;
using Tanks.UI.TankModel;
using Tanks.Utils;

namespace Tanks.UI.Menus.LobbyBrowser
{
    public sealed class LobbyBrowserViewModel : IViewModel
    {
        public LobbyBrowserViewModel(
            ILobbyService service,
            ILocalizationDatabase localization,
            ILoadingScreenService loading,
            IDialogScreenService dialog,
            IMenuSectionService navigation,
            ITankModelService tankModelService)
        {
            _service = service;
            _localization = localization;
            _loading = loading;
            _dialog = dialog;
            _navigation = navigation;
            _tankModelService = tankModelService;
        }

        public BindableProperty<string> QueryName { get; } = new("");
        public BindableProperty<string> Status { get; } = new("");
        public BindableProperty<bool> IsLoading { get; } = new();
        public BindableProperty<LobbySearchState[]> FoundLobbies { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuSectionService _navigation;
        private readonly ITankModelService _tankModelService;

        public void Back()
        {
            _navigation.OpenSection(MenuSectionType.Play, instant: false);
        }

        public async UniTask RefreshAsync()
        {
            _localization.TryGet("Menus", "menu.find-lobby.searching", out var searching);

            Status.Value = searching?.Get() ?? "";
            IsLoading.Value = true;
            FoundLobbies.Value = null;

            var lobbies = await _service.FindLobbiesAsync(QueryName.Value);
            IsLoading.Value = false;

            if (lobbies.IsSuccess == false)
            {
                _localization.TryGet("Menus", "menu.find-lobby.not-found", out var notFound);
                Status.Value = notFound?.Get() ?? "";
                _dialog.ShowErrorFromCode("error.lobby", lobbies.ErrorCode);
                return;
            }

            FoundLobbies.Value = lobbies.Value.ToArray();

            _localization.TryGet("Menus", FoundLobbies.Value.Length > 0
                ? "menu.find-lobby.found"
                : "menu.find-lobby.not-found", out var found);

            found?.AddVariable("count", FoundLobbies.Value.Length);
            Status.Value = found?.Get() ?? "";
        }

        public async UniTask JoinLobbyAsync(LobbyId id)
        {
            _navigation.OpenSection(MenuSectionType.None, instant: true);
            _tankModelService.StartDrivingInPlace();

            _localization.TryGet("Menus", "menu.lobby.joining", out ILocalizedString joining);
            _loading.Begin(joining);

            ResultType<LobbyState> result = await _service.JoinLobbyAsync(id);

            _loading.End();
            _tankModelService.StopDrivingInPlace();

            if (result.IsSuccess)
            {
                _tankModelService.DriveByCurve(0, 0, instant: false);
                _navigation.OpenSection(MenuSectionType.LobbyRoom, instant: true, menuDelay: 2.5f);
                return;
            }

            _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
            _navigation.OpenSection(MenuSectionType.LobbyBrowser);
        }
    }
}
