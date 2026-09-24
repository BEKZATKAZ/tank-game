using System;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbySectionsViewModel : IViewModel, IDisposable
    {
        public LobbySectionsViewModel(
            ILobbyService service,
            ILocalizationDatabase localization,
            ISessionSettingsService settings,
            IDialogScreenService dialog,
            IMenuNavigation navigation,
            IConnectToServerUseCase connectToServer)
        {
            _service = service;
            _localization = localization;
            _settings = settings;
            _dialog = dialog;
            _navigation = navigation;
            _connectToServer = connectToServer;

            LobbyName = service.CurrentLobbyState.Name;

            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.JoinCode.Bind(OnJoinCodeUpdated);
            _service.CurrentLobbyState.ServerConfirmedSettings.Bind(OnSettingsChanged);
            _service.LobbyDeleted += OnLobbyDeleted;
        }

        public IReadOnlyBindableProperty<string> LobbyName { get; }
        public BindableProperty<string> Status { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly ISessionSettingsService _settings;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;
        private readonly IConnectToServerUseCase _connectToServer;

        public void Dispose()
        {
            _service.LobbyDeleted -= OnLobbyDeleted;
            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.JoinCode.Unbind(OnJoinCodeUpdated);
            _service.CurrentLobbyState.ServerConfirmedSettings.Unbind(OnSettingsChanged);
        }

        private void OnLobbyDeleted()
        {
            _dialog.ShowErrorFromCode("error.lobby", "lobby-disbanded");
            _navigation.Navigate(MenuType.Main);
        }

        private void OnSettingsChanged(SessionSettings settings)
        {
            _settings.SetSettings(settings);
        }

        private async void OnJoinCodeUpdated(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode)) return;

            Debug.LogFormat("Connecting to server: '{0}'", joinCode);

            await _connectToServer.ConnectAsync(joinCode);
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
