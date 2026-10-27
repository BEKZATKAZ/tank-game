using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Lobbies;
using Tanks.UI.Menus.Dialogs;

namespace Tanks.UI.Tests.Menus.Lobbies
{
    public class LobbyChatTests : ViewModelTestBase<LobbyChatViewModel>
    {
        private ILobbyService _service;
        private ILocalizationDatabase _localization;
        private LobbyState _lobby;
        private ILobbyChatService _chat;
        private IDialogScreenService _dialog;

        protected override LobbyChatViewModel CreateViewModel()
        {
            _service = Substitute.For<ILobbyService>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby-id");
            _lobby.Name.Value = "Test lobby";
            _service.CurrentLobbyState.Returns(_lobby);

            _chat = Substitute.For<ILobbyChatService>();
            _chat.Messages.Returns(Array.Empty<LobbyChatMessage>());
            _dialog = Substitute.For<IDialogScreenService>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Success()));
            _chat.SendAsync(Arg.Any<LobbyId>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Success()));
            _chat.DisconnectAsync().Returns(Task.FromResult(Result.Success()));
            return new LobbyChatViewModel(_service, _localization, _chat, _dialog);
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

        [TestCase("")]
        [TestCase("   ")]
        public async Task Send_EmptyDraft_DoesNothing(string draft)
        {
            ViewModel.Draft.Value = draft;

            await ViewModel.SendAsync();

            await _chat.DidNotReceive().SendAsync(Arg.Any<LobbyId>(), Arg.Any<string>());
        }

        [TestCase(500, true)]
        [TestCase(501, false)]
        public void Draft_EnforcesLengthLimit(int length, bool expected)
        {
            ViewModel.Draft.Value = new string('a', length);

            Assert.That(ViewModel.CanSend.Value, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Send_RestoresFailedDraft(bool success)
        {
            ViewModel.Draft.Value = "hello";
            _chat.SendAsync(Arg.Any<LobbyId>(), "hello").Returns(Task.FromResult(
                success ? Result.Success() : Result.Failure("chat-send-failed")));

            await ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.EqualTo(success ? "" : "hello"));
            if (success == false) _dialog.Received(1).ShowErrorFromCode("error.lobby", "chat-send-failed");
        }

        [Test]
        public async Task Send_InFlight_PreventsDuplicatesAndPreservesNewDraft()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _chat.SendAsync(Arg.Any<LobbyId>(), Arg.Any<string>()).Returns(pending.Task);
            ViewModel.Draft.Value = "first";

            Task sending = ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.Empty);

            ViewModel.Draft.Value = "next";
            await ViewModel.SendAsync();
            pending.SetResult(Result.Success());
            await sending;

            await _chat.Received(1).SendAsync(Arg.Any<LobbyId>(), "first");
            Assert.That(ViewModel.Draft.Value, Is.EqualTo("next"));
        }

        [Test]
        public void Messages_DeduplicateByIdAndIgnoreOtherLobbies()
        {
            var message = new LobbyChatMessage { Id = "one", LobbyId = _lobby.Id.Value, Content = "hello" };

            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(message);
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(message);
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(
                new LobbyChatMessage { Id = "two", LobbyId = new LobbyId("other") });

            Assert.That(ViewModel.Messages.Value.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task LobbyChange_ClearsMessagesAndDraft()
        {
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(
                new LobbyChatMessage { Id = "one", LobbyId = _lobby.Id.Value });
            ViewModel.Draft.Value = "draft";

            _lobby.Id.Value = default;

            Assert.That(ViewModel.Messages.Value, Is.Empty);
            Assert.That(ViewModel.Draft.Value, Is.Empty);
            Assert.That(ViewModel.CanSend.Value, Is.False);
            await _chat.DidNotReceive().DisconnectAsync();
        }

        [Test]
        public async Task Dispose_UnsubscribesAndIgnoresPendingSend()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(pending.Task);
            ViewModel.Draft.Value = "hello";
            Task sending = ViewModel.SendAsync();

            ViewModel.Dispose();
            pending.SetResult(Result.Failure("chat-unavailable"));
            await sending;

            Assert.That(ViewModel.CanSend.Value, Is.False);
            _dialog.DidNotReceive().ShowErrorFromCode(Arg.Any<string>(), Arg.Any<string>());
            await _chat.DidNotReceive().DisconnectAsync();
        }

        [Test]
        public async Task Send_ConnectFailure_ShowsDialog()
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));
            ViewModel.Draft.Value = "hello";

            await ViewModel.SendAsync();

            _dialog.Received(1).ShowErrorFromCode("error.lobby", "chat-unavailable");
            Assert.That(ViewModel.CanSend.Value, Is.True);
        }

        [Test]
        public async Task SendFailure_RestoresBeforeNewDraftAndPreservesOverflow()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _chat.SendAsync(Arg.Any<LobbyId>(), Arg.Any<string>()).Returns(pending.Task);
            string failed = new string('a', 500);
            ViewModel.Draft.Value = failed;

            Task sending = ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.Empty);

            ViewModel.Draft.Value = "new message";
            pending.SetResult(Result.Failure("chat-send-failed"));
            await sending;

            Assert.That(ViewModel.Draft.Value, Is.EqualTo(failed + "\nnew message"));
            Assert.That(ViewModel.CanSend.Value, Is.False);
            _dialog.Received(1).ShowErrorFromCode("error.lobby", "chat-send-failed");
        }

        [Test]
        public async Task FailedConnection_SendRetriesAndClearsSuccessfulDraft()
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));
            ViewModel.Draft.Value = "hello";
            await ViewModel.SendAsync();

            Assert.That(ViewModel.CanSend.Value, Is.True);

            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Success()));

            await ViewModel.SendAsync();

            await _chat.Received(1).SendAsync(Arg.Is<LobbyId>(id => id.Value == "lobby-id"), "hello");
            Assert.That(ViewModel.Draft.Value, Is.Empty);
        }

        [Test]
        public async Task Send_ReconnectFailure_PreservesDraftAndAllowsRetry()
        {
            ViewModel.Draft.Value = "hello";
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));

            await ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.EqualTo("hello"));
            Assert.That(ViewModel.CanSend.Value, Is.True);
            await _chat.DidNotReceive().SendAsync(Arg.Any<LobbyId>(), Arg.Any<string>());
        }

        [Test]
        public async Task ShutdownCancellation_DoesNotOpenErrorDialog()
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("cancelled")));

            ViewModel.Draft.Value = "hello";
            await ViewModel.SendAsync();

            _dialog.DidNotReceive().ShowErrorFromCode(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void CachedHistory_IsAvailableWithoutConnectingAgain()
        {
            var history = new LobbyChatMessage
            {
                Id = "old", LobbyId = _lobby.Id.Value, Content = "history", SenderId = "remote", IsHistory = true
            };
            _chat.Messages.Returns(new[] { history });

            using var model = new LobbyChatViewModel(_service, _localization, _chat, _dialog);
            _chat.MessageReceived += Raise.Event<Action<LobbyChatMessage>>(history);

            Assert.That(model.Messages.Value.Count, Is.EqualTo(1));
            Assert.That(model.Messages.Value[0], Is.SameAs(history));
            _chat.DidNotReceive().ConnectAsync(Arg.Any<LobbyId>());
        }

        [Test]
        public async Task Send_ClearsBeforeConnectionCompletes()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(pending.Task);
            ViewModel.Draft.Value = "hello";

            Task sending = ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.Empty);
            Assert.That(ViewModel.CanSend.Value, Is.False);

            pending.SetResult(Result.Success());
            await sending;
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
