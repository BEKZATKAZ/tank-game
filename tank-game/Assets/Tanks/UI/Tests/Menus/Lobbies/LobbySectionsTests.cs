using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core.Lobbies;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Localization;
using Tanks.UI.Menus;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Lobbies;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbySectionsTests : ViewModelTestBase<LobbySectionsViewModel>
    {
        private ILobbyService _service;
        private ILocalizationDatabase _localization;
        private IDialogScreenService _dialog;
        private IMenuNavigation _navigation;
        private IConnectToServerUseCase _connectToServer;
        private LobbyState _lobby;
        private ILobbyChatService _chat;
        private IAuthService _auth;

        protected override LobbySectionsViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _dialog = Substitute.For<IDialogScreenService>();
            _navigation = Substitute.For<IMenuNavigation>();
            _connectToServer = Substitute.For<IConnectToServerUseCase>();
            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby");
            _service.CurrentLobbyState.Returns(_lobby);

            _chat = Substitute.For<ILobbyChatService>();
            _chat.Messages.Returns(Array.Empty<LobbyChatMessage>());

            _auth = Substitute.For<IAuthService>();
            var profile = new PlayerProfileState();
            profile.UserId.Value = "local";
            _auth.CurrentProfile.Returns(profile);

            return new LobbySectionsViewModel(
                _service, _localization, _dialog, _navigation, _connectToServer, _chat, _auth);
        }

        [Test]
        public void LobbyName_UsesCurrentLobby()
        {
            Assert.AreSame(_lobby.Name, ViewModel.LobbyName);
        }

        [TestCase(LobbyStatus.PreparingServer, "menu.lobby.preparing-server")]
        [TestCase(LobbyStatus.Connecting, "menu.lobby.connecting")]
        public void Status_UsesLocalizedMessage(LobbyStatus status, string entry)
        {
            _localization.TryGet("Menus", entry, out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    ILocalizedString text = Substitute.For<ILocalizedString>();
                    text.Get().Returns("Localized status");
                    args[2] = text;
                    return true;
                });

            _lobby.Status.Value = status;
            Assert.AreEqual("Localized status", ViewModel.Status.Value);
        }

        [Test]
        public void ConfirmedSettings_ConnectsToServer()
        {
            var settings = new SessionSettings { DesiredPlayerCount = 4 };
            _lobby.JoinCode.Value = "join-code";

            _lobby.ServerConfirmedSettings.Value = settings;

            _connectToServer.Received(1).Load("join-code", settings);
        }

        [Test]
        public void DefaultSettings_DoNotConnect()
        {
            _lobby.ServerConfirmedSettings.Value = new SessionSettings { MaxScore = 10 };
            _connectToServer.DidNotReceiveWithAnyArgs().Load(default, default);
        }

        [Test]
        public void LobbyDeleted_ShowsErrorAndReturnsToMain()
        {
            _service.LobbyDeleted += Raise.Event<Action>();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "lobby-disbanded");
            _navigation.Received(1).Navigate(MenuType.Main);
        }

        [TestCase(false, false, "remote", 1)]
        [TestCase(true, false, "remote", 0)]
        [TestCase(false, true, "remote", 0)]
        [TestCase(false, false, "local", 0)]
        [TestCase(false, false, null, 0)]
        public void Messages_NotifyOnlyForOtherPlayersLiveMessages(bool history, bool open, string sender, int expected)
        {
            ViewModel.SetChatOpen(open);

            Receive("one", sender, history);

            Assert.That(ViewModel.UnreadCount.Value, Is.EqualTo(expected));
        }

        [Test]
        public void OpeningChat_ClearsUnreadAndDuplicatesStayRead()
        {
            Receive("one");
            Receive("one");
            Assert.That(ViewModel.UnreadCount.Value, Is.EqualTo(1));

            ViewModel.SetChatOpen(true);
            Receive("two");
            ViewModel.SetChatOpen(false);
            Receive("one");
            Receive("two");

            Assert.That(ViewModel.UnreadCount.Value, Is.Zero);
        }

        [Test]
        public void LobbyChange_ClearsUnreadAndRejectsPreviousLobby()
        {
            Receive("one");

            _lobby.Id.Value = new LobbyId("next");
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(new LobbyChatMessage
            {
                Id = "late", LobbyId = new LobbyId("lobby"), SenderId = "remote"
            });

            Assert.That(ViewModel.UnreadCount.Value, Is.Zero);
            Assert.That(ViewModel.IsChatOpen.Value, Is.False);
        }

        [Test]
        public void CachedMessages_DoNotProduceUnreadOnReplay()
        {
            _chat.Messages.Returns(new[]
            {
                new LobbyChatMessage { Id = "one", LobbyId = new LobbyId("next"), SenderId = "remote", IsHistory = true }
            });

            _lobby.Id.Value = new LobbyId("next");
            Receive("one");

            Assert.That(ViewModel.UnreadCount.Value, Is.Zero);
        }

        [Test]
        public void UnreadCount_SaturatesFor99PlusBadge()
        {
            for (int i = 0; i < 105; i++) Receive(i.ToString());

            Assert.That(ViewModel.UnreadCount.Value, Is.EqualTo(100));
        }

        [Test]
        public void LiveMessageReceivedDuringEntry_IsUnreadWhenLobbyOpens()
        {
            _chat.Messages.Returns(new[]
            {
                new LobbyChatMessage { Id = "live", LobbyId = new LobbyId("next"), SenderId = "remote" }
            });

            _lobby.Id.Value = new LobbyId("next");

            Assert.That(ViewModel.UnreadCount.Value, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_StopsUnreadUpdates()
        {
            ViewModel.Dispose();

            Receive("one");

            Assert.That(ViewModel.UnreadCount.Value, Is.Zero);
        }

        private void Receive(string id, string sender = "remote", bool history = false)
        {
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(new LobbyChatMessage
            {
                Id = id, LobbyId = _lobby.Id.Value, SenderId = sender, IsHistory = history
            });
        }
    }
}
