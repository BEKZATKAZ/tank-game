using System;
using System.Collections.Generic;
using Tanks.Core;
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
            IConnectToServerUseCase connectToServer,
            ILobbyChatService chat,
            IAuthService auth)
        {
            _service = service;
            _localization = localization;
            _dialog = dialog;
            _navigation = navigation;
            _connectToServer = connectToServer;
            _chat = chat;
            _auth = auth;

            LobbyName = service.CurrentLobbyState.Name;

            _service.CurrentLobbyState.Status.Bind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Bind(OnSettingsChanged);
            _service.LobbyDeleted += OnLobbyDeleted;
            _service.CurrentLobbyState.Id.Bind(OnLobbyChanged);
            _chat.MessageReceived += OnMessageReceived;
        }

        public IReadOnlyBindableProperty<string> LobbyName { get; }
        public BindableProperty<string> Status { get; } = new();
        public BindableProperty<bool> IsChatOpen { get; } = new();
        public BindableProperty<bool> IsSettingsOpen { get; } = new();
        public BindableProperty<int> UnreadCount { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;
        private readonly IMenuNavigation _navigation;
        private readonly IConnectToServerUseCase _connectToServer;
        private readonly ILobbyChatService _chat;
        private readonly IAuthService _auth;

        private readonly HashSet<string> _received = new();

        public void Dispose()
        {
            _service.LobbyDeleted -= OnLobbyDeleted;
            _service.CurrentLobbyState.Status.Unbind(OnStatusChanged);
            _service.CurrentLobbyState.ServerConfirmedSettings.Unbind(OnSettingsChanged);
            _service.CurrentLobbyState.Id.Unbind(OnLobbyChanged);
            _chat.MessageReceived -= OnMessageReceived;
        }

        public void SetChatOpen(bool open)
        {
            IsSettingsOpen.Value = false;
            IsChatOpen.Value = open;
            if (open) UnreadCount.Value = 0;
        }

        public void OpenSettings()
        {
            IsChatOpen.Value = false;
            IsSettingsOpen.Value = true;
        }

        private void OnLobbyChanged(LobbyId id)
        {
            _received.Clear();
            UnreadCount.Value = 0;
            IsChatOpen.Value = false;
            IsSettingsOpen.Value = false;

            foreach (LobbyChatMessage message in _chat.Messages)
            {
                OnMessageReceived(message);
            }
        }

        private void OnMessageReceived(LobbyChatMessage message)
        {
            if (message.LobbyId.Value != _service.CurrentLobbyState.Id.Value.Value) return;
            if (string.IsNullOrEmpty(message.Id) || _received.Add(message.Id) == false) return;
            if (message.IsHistory || IsChatOpen.Value || string.IsNullOrEmpty(message.SenderId)) return;
            if (message.SenderId == _auth.CurrentProfile.UserId.Value) return;

            UnreadCount.Value = Math.Min(UnreadCount.Value + 1, 100);
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

            // Prevent loading gameplay on any lobby join
            if (string.IsNullOrWhiteSpace(joinCode)) return;

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
