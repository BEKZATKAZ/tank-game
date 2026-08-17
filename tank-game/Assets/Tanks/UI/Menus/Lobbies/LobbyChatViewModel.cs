using System;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.Utils;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyChatViewModel : IViewModel, IDisposable
    {
        public LobbyChatViewModel(ILobbyService service, ILocalizationDatabase localization)
        {
            _service = service;
            _localization = localization;

            _service.PlayerJoined += OnPlayerJoined;
            _service.PlayerLeft += OnPlayerLeft;
        }

        public BindableProperty<string> CurrentServerMessage { get; } = new();

        private readonly ILobbyService _service;
        private readonly ILocalizationDatabase _localization;

        public void Dispose()
        {
            _service.PlayerJoined -= OnPlayerJoined;
            _service.PlayerLeft -= OnPlayerLeft;
        }

        public void DisplayWelcomeMessage()
        {
            _localization.TryGet("Menus", "menu.lobby.chat-welcome", out var text);
            text?.AddVariable("name", _service.CurrentLobbyState.Name.Value);
            CurrentServerMessage.Value = text?.Get() ?? "";
        }

        private void OnPlayerJoined(LobbyPlayerState player)
        {
            _localization.TryGet("Menus", "menu.lobby.chat-player-joined", out var text);
            text?.AddVariable("username", player.UserId);
            CurrentServerMessage.Value = text?.Get() ?? "";
        }

        private void OnPlayerLeft(LobbyPlayerState player)
        {
            _localization.TryGet("Menus", "menu.lobby.chat-player-disconnected", out var text);
            text?.AddVariable("username", player.UserId);
            CurrentServerMessage.Value = text?.Get() ?? "";
        }
    }
}
