using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbyChatTests : ViewModelTestBase<LobbyChatViewModel>
    {
        private ILobbyService _service;
        private ILocalizationDatabase _localization;
        private LobbyState _lobby;

        protected override LobbyChatViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby-id");
            _lobby.Name.Value = "Test lobby";
            _service.CurrentLobbyState.Returns(_lobby);
            return new LobbyChatViewModel(_service, _localization);
        }

        [Test]
        public void LobbyId_UsesCurrentLobby()
        {
            Assert.AreSame(_lobby.Id, ViewModel.LobbyId);
        }

        [Test]
        public void DisplayWelcomeMessage_UsesLobbyName()
        {
            ILocalizedString text = MockLocalization("menu.lobby.chat-welcome", "Welcome");

            ViewModel.DisplayWelcomeMessage();

            text.Received(1).AddVariable("name", "Test lobby");
            Assert.AreEqual("Welcome", ViewModel.CurrentServerMessage.Value);
        }

        [TestCase(true, "menu.lobby.chat-player-joined")]
        [TestCase(false, "menu.lobby.chat-player-disconnected")]
        public void PlayerEvent_UpdatesMessage(bool joined, string entry)
        {
            ILocalizedString text = MockLocalization(entry, "Player update");
            var player = new LobbyPlayerState
            {
                UserId = "player-id",
                Data = new Dictionary<string, string>()
            };

            if (joined) _service.PlayerJoined += Raise.Event<Action<LobbyPlayerState>>(player);
            else _service.PlayerLeft += Raise.Event<Action<LobbyPlayerState>>(player);

            text.Received(1).AddVariable("username", "player-id");
            Assert.AreEqual("Player update", ViewModel.CurrentServerMessage.Value);
        }

        private ILocalizedString MockLocalization(string entry, string value)
        {
            ILocalizedString text = Substitute.For<ILocalizedString>();
            text.Get().Returns(value);
            _localization.TryGet("Menus", entry, out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    args[2] = text;
                    return true;
                });
            return text;
        }
    }
}
