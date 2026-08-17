using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Core.Lobbies;
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
            IMenuNavigation navigation)
        {
            _service = service;
            _localization = localization;
            _loading = loading;
            _dialog = dialog;
            _navigation = navigation;
        }

        public BindableProperty<string> QueryName { get; } = new("");
        public BindableProperty<string> Status { get; } = new("");
        public BindableProperty<LobbySearchState[]> FoundLobbies { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly ILoadingScreenService _loading;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;

        public async UniTask RefreshAsync()
        {
            _localization.TryGet("Menus", "menu.find-lobby.searching", out var searching);

            Status.Value = searching?.Get() ?? "";
            FoundLobbies.Value = null;

            var lobbies = await _service.FindLobbiesAsync(QueryName.Value);

            if (lobbies.IsSuccess == false)
            {
                _localization.TryGet("Menus", "menu.find-lobby.not-found", out var notFound);
                Status.Value = notFound?.Get() ?? "";
                _dialog.ShowErrorFromCode("error.lobby", lobbies.ErrorCode);
                return;
            }

            FoundLobbies.Value = lobbies.Value.ToArray();
            _localization.TryGet("Menus", "menu.find-lobby.found", out var found);

            found?.AddVariable("count", FoundLobbies.Value.Length);
            Status.Value = found?.Get() ?? "";
        }

        public async UniTask JoinLobbyAsync(LobbyAddress address)
        {
            _localization.TryGet("Menus", "menu.lobby.joining", out ILocalizedString joining);
            _loading.Begin(joining);

            var result = await _service.JoinLobbyAsync(address);
            _loading.End();

            if (result.IsSuccess) _navigation.Navigate(MenuType.Lobby);
            else _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
        }
    }
}
