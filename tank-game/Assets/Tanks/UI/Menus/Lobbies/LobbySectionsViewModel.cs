using System;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbySectionsViewModel : IViewModel, IDisposable
    {
        public LobbySectionsViewModel(
            ILobbyService service,
            ILocalizationDatabase localization,
            IDialogScreenService dialog,
            IMenuNavigation navigation,
            IConnectToServerUseCase connectToServer)
        {
            _service = service;
            _localization = localization;
            _dialog = dialog;
            _navigation = navigation;
            _connectToServer = connectToServer;

            LobbyName = service.CurrentLobbyState.Name;

            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Bind(OnSettingsChanged);
            _service.LobbyDeleted += OnLobbyDeleted;
        }

        public IReadOnlyBindableProperty<string> LobbyName { get; }
        public BindableProperty<string> Status { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;
        private readonly IConnectToServerUseCase _connectToServer;

        public void Dispose()
        {
            _service.LobbyDeleted -= OnLobbyDeleted;
            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Unbind(OnSettingsChanged);
        }

        private void OnLobbyDeleted()
        {
            _dialog.ShowErrorFromCode("error.lobby", "lobby-disbanded");
            _navigation.Navigate(MenuType.Main);
        }

        private void OnSettingsChanged(SessionSettings settings)
        {
            // Discard default settings
            if (settings.DesiredPlayerCount == 0) return;

            string joinCode = _service.CurrentLobbyState.JoinCode.Value;
            _connectToServer.Load(joinCode, settings);
        }

        private void OnStatusChanged(LobbyStatus status)
        {
            string entryName = status switch
            {
                LobbyStatus.PreparingServer => "menu.lobby.preparing-server",
                LobbyStatus.Connecting => "menu.lobby.connecting",
                _ => null
            };

            _localization.TryGet("Menus", entryName, out ILocalizedString localized);
            Status.Value = localized?.Get() ?? "";
        }
    }
}
