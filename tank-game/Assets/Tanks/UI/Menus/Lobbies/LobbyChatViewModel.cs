using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyChatViewModel : IViewModel, IDisposable
    {
        public LobbyChatViewModel(
            ILobbyService service, ILocalizationDatabase localization,
            ILobbyChatService chat, IDialogScreenService dialog)
        {
            _service = service;
            _localization = localization;
            _chat = chat;
            _dialog = dialog;

            _service.PlayerJoined += OnPlayerJoined;
            _service.PlayerLeft += OnPlayerLeft;
            _chat.MessageReceived += OnMessageReceived;

            Draft.Bind(OnDraftChanged);
            LobbyId.Bind(OnLobbyChanged);
        }

        public IReadOnlyBindableProperty<string> CurrentServerMessage => _currentServerMessage;
        public IReadOnlyBindableProperty<LobbyId> LobbyId => _service.CurrentLobbyState.Id;
        public IReadOnlyBindableProperty<IReadOnlyList<LobbyChatMessage>> Messages => _messages;

        public BindableProperty<string> Draft { get; } = new("");
        public BindableProperty<bool> CanSend { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly ILobbyChatService _chat;
        private readonly IDialogScreenService _dialog;

        private readonly BindableProperty<string> _currentServerMessage = new();
        private readonly BindableProperty<IReadOnlyList<LobbyChatMessage>> _messages = new(Array.Empty<LobbyChatMessage>());
        private readonly HashSet<string> _received = new();

        private bool _sending;
        private bool _disposed;
        private int _generation;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _generation++;
            CanSend.Value = false;

            _service.PlayerJoined -= OnPlayerJoined;
            _service.PlayerLeft -= OnPlayerLeft;
            _chat.MessageReceived -= OnMessageReceived;

            LobbyId.Unbind(OnLobbyChanged);
            Draft.Unbind(OnDraftChanged);
        }

        private void OnDraftChanged(string draft)
        {
            CanSend.Value = _disposed == false && _sending == false &&
                string.IsNullOrEmpty(LobbyId.Value.Value) == false &&
                string.IsNullOrWhiteSpace(draft) == false && draft.Length <= ILobbyChatService.MaxMessageLength;
        }

        private void OnLobbyChanged(LobbyId id)
        {
            _generation++;
            _sending = false;

            _received.Clear();
            _messages.Value = Array.Empty<LobbyChatMessage>();
            Draft.Value = "";
            OnDraftChanged(Draft.Value);

            if (string.IsNullOrEmpty(id.Value)) return;

            DisplayWelcomeMessage();

            foreach (LobbyChatMessage message in _chat.Messages)
            {
                OnMessageReceived(message);
            }
        }

        public async Task SendAsync()
        {
            if (CanSend.Value == false) return;

            string draft = Draft.Value;
            LobbyId id = LobbyId.Value;
            int generation = _generation;

            _sending = true;
            Draft.Value = "";
            OnDraftChanged(Draft.Value);

            try
            {
                ResultType result = await _chat.ConnectAsync(id);
                if (_disposed || generation != _generation) return;

                if (result.IsSuccess) result = await _chat.SendAsync(id, draft);
                if (_disposed || generation != _generation) return;

                if (result.IsSuccess) return;

                Draft.Value = string.IsNullOrEmpty(Draft.Value) ? draft : draft + "\n" + Draft.Value;

                if (result.ErrorCode != "cancelled")
                {
                    _dialog.ShowErrorFromCode("error.lobby", result.ErrorCode);
                }
            }
            finally
            {
                if (_disposed == false && generation == _generation)
                {
                    _sending = false;
                    OnDraftChanged(Draft.Value);
                }
            }
        }

        private void OnMessageReceived(LobbyChatMessage message)
        {
            if (_disposed || message.LobbyId.Value != LobbyId.Value.Value) return;
            if (string.IsNullOrEmpty(message.Id) || _received.Add(message.Id) == false) return;

            AddMessage(message);
        }

        private void AddMessage(LobbyChatMessage message)
        {
            var messages = new List<LobbyChatMessage>(_messages.Value) { message };
            _messages.Value = messages;
        }

        private void AddServerMessage(string content)
        {
            _currentServerMessage.Value = content;
            if (string.IsNullOrWhiteSpace(content)) return;

            AddMessage(new LobbyChatMessage { Content = content });
        }

        public void DisplayWelcomeMessage()
        {
            _localization.TryGet("Menus", "menu.lobby.chat-welcome", out var text);
            text?.AddVariable("name", _service.CurrentLobbyState.Name.Value);

            AddServerMessage(text?.Get() ?? "");
        }

        private void OnPlayerJoined(LobbyPlayerState player)
        {
            _localization.TryGet("Menus", "menu.lobby.chat-player-joined", out var text);
            text?.AddVariable("username", player.DisplayName);

            AddServerMessage(text?.Get() ?? "");
        }

        private void OnPlayerLeft(LobbyPlayerState player)
        {
            _localization.TryGet("Menus", "menu.lobby.chat-player-disconnected", out var text);
            text?.AddVariable("username", player.DisplayName);

            AddServerMessage(text?.Get() ?? "");
        }
    }
}
