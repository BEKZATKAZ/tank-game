using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyBrowserViewModel : IViewModel
    {
        public LobbyBrowserViewModel(
            ILobbyService service,
            ILocalizationDatabase localization,
            IDialogScreenService dialog)
        {
            _service = service;
            _localization = localization;
            _dialog = dialog;
        }

        public BindableProperty<string> QueryName { get; } = new("");
        public BindableProperty<string> Status { get; } = new("");
        public BindableProperty<LobbySearchState[]> FoundLobbies { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;

        public async UniTaskVoid RefreshAsync()
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
    }
}
