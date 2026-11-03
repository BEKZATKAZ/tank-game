using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.LobbyRoom
{
    public sealed class LobbyChatViewModel : IViewModel, IDisposable
    {
        public LobbyChatViewModel(
            ILobbyService service, ILocalizationDatabase localization, IDialogScreenService dialog)
        {
            _service = service;
            _localization = localization;
            _dialog = dialog;

            _service.PlayerJoined += OnPlayerJoined;
            _service.PlayerLeft += OnPlayerLeft;

            Draft.Bind(OnDraftChanged);
            LobbyId.Bind(OnLobbyChanged);
            _service.CurrentLobbyState.Players.Bind(OnPlayersChanged);
        }

        public IReadOnlyBindableProperty<string> CurrentServerMessage => _currentServerMessage;
        public IReadOnlyBindableProperty<LobbyId> LobbyId => _service.CurrentLobbyState.Id;
        public IReadOnlyBindableProperty<IReadOnlyList<LobbyChatMessage>> Messages => _messages;

        public BindableProperty<string> Draft { get; } = new("");
        public BindableProperty<bool> CanSend { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;
        private readonly IDialogScreenService _dialog;

        private readonly BindableProperty<string> _currentServerMessage = new();
        private readonly BindableProperty<IReadOnlyList<LobbyChatMessage>> _messages = new(Array.Empty<LobbyChatMessage>());
        private readonly HashSet<string> _received = new();
        private readonly Dictionary<string, string> _observed = new();

        private bool _sending;
        private bool _disposed;
        private int _generation;
        private bool _baselinePending;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _generation++;
            CanSend.Value = false;

            _service.PlayerJoined -= OnPlayerJoined;
            _service.PlayerLeft -= OnPlayerLeft;
            _service.CurrentLobbyState.Players.Unbind(OnPlayersChanged);

            LobbyId.Unbind(OnLobbyChanged);
            Draft.Unbind(OnDraftChanged);
        }

        private void OnDraftChanged(string draft)
        {
            CanSend.Value = _disposed == false && _sending == false &&
                string.IsNullOrEmpty(LobbyId.Value.Value) == false &&
                string.IsNullOrWhiteSpace(draft) == false && draft.Length <= LobbyChatMessage.MaxMessageLength;
        }

        private void OnLobbyChanged(LobbyId id)
        {
            _generation++;
            _sending = false;

            _received.Clear();
            _observed.Clear();
            _messages.Value = Array.Empty<LobbyChatMessage>();
            _currentServerMessage.Value = "";
            _baselinePending = string.IsNullOrEmpty(id.Value) == false;
            Draft.Value = "";
            OnDraftChanged(Draft.Value);
        }

        public async Task SendAsync()
        {
            if (CanSend.Value == false) return;

            string draft = Draft.Value;
            int generation = _generation;

            _sending = true;
            Draft.Value = "";
            OnDraftChanged(Draft.Value);

            try
            {
                ResultType result = await _service.SendMessageAsync(draft);
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

        private void OnPlayersChanged(IDictionary<string, LobbyPlayerState> players)
        {
            if (_disposed || string.IsNullOrEmpty(LobbyId.Value.Value)) return;

            bool baseline = _baselinePending;
            _baselinePending = false;
            if (baseline) DisplayWelcomeMessage();

            foreach (LobbyPlayerState player in players.Values)
            {
                string json = null;
                player.Data?.TryGetValue("LastMessage", out json);
                bool known = _observed.TryGetValue(player.UserId, out string previous);
                _observed[player.UserId] = json;

                if (string.IsNullOrEmpty(json)) continue;

                try
                {
                    var payload = JsonConvert.DeserializeObject<LobbyChatMessage.Payload>(json);
                    if (payload == null || string.IsNullOrEmpty(payload.Id) ||
                        string.IsNullOrWhiteSpace(payload.Text) || payload.Text.Length > LobbyChatMessage.MaxMessageLength)
                    {
                        continue;
                    }

                    if (_received.Add(player.UserId + ":" + payload.Id) == false) continue;
                    if (baseline || known == false || previous == json) continue;

                    AddMessage(new LobbyChatMessage
                    {
                        LobbyId = LobbyId.Value,
                        Id = payload.Id,
                        SenderId = player.UserId,
                        Username = player.DisplayName,
                        Content = payload.Text,
                        ReceivedTime = DateTime.UtcNow
                    });
                }
                catch (JsonException)
                {
                }
            }
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
