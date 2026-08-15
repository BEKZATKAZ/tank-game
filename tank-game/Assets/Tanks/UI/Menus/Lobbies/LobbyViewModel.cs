using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyViewModel : IViewModel
    {
        public LobbyViewModel(
            ILobbyService service,
            IMenuNavigation navigation,
            IDialogScreenService dialog)
        {
            _service = service;
            _navigation = navigation;
            _dialog = dialog;

            Players = service.CurrentLobbyState.Players;
            LobbyName = service.CurrentLobbyState.Name;
            MaxMemberCount = service.CurrentLobbyState.MaxPlayerCount;
        }

        public IReadOnlyBindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; }
        public IReadOnlyBindableProperty<string> LobbyName { get; }
        public IReadOnlyBindableProperty<int> MaxMemberCount { get; }

        private readonly ILobbyService _service;
        private readonly IMenuNavigation _navigation;
        private readonly IDialogScreenService _dialog;

        public void StartGame()
        {

        }

        public void LeaveLobbyConfirmation()
        {
            _dialog.ShowConfirmation("leave-lobby", onYes: () =>
            {
                _service.LeaveLobby();
                _navigation.Navigate(MenuType.Main);
            });
        }

        public bool TryRenameLobby(string newName)
        {
            if (LobbyName.Value == newName || string.IsNullOrWhiteSpace(newName)) return false;
            _service.RenameLobby(newName.Trim());
            return true;
        }
    }
}
