using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyBrowserViewModel : IViewModel
    {
        public LobbyBrowserViewModel(
            ILobbyService service,
            ILocalizationDatabase localization,
            ILoadingScreenService loading,
            IDialogScreenService dialog,
            IMenuNavigation navigation,
            IEnterLobbyUseCase enterLobby)
        {
            _service = service;
            _localization = localization;
            _loading = loading;
            _dialog = dialog;
            _navigation = navigation;
            _enterLobby = enterLobby;
        }

        public BindableProperty<string> QueryName { get; } = new("");
        public BindableProperty<string> Status { get; } = new("");
        public BindableProperty<bool> IsLoading { get; } = new();
        public BindableProperty<LobbySearchState[]> FoundLobbies { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;
        private readonly IEnterLobbyUseCase _enterLobby;

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
            _localization.TryGet("Menus", "menu.lobby.joining", out ILocalizedString joining);
            _loading.Begin(joining);

            ResultType<LobbyState> result;

            try
            {
                result = await _enterLobby.JoinAsync(id);
            }
            finally
            {
                _loading.End();
            }

            if (result.IsSuccess) _navigation.Navigate(MenuType.Lobby);
            else if (result.ErrorCode != "cancelled") _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
        }
    }
}
