using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.LobbyRoom;

namespace Tanks.UI.Tests.Menus.LobbyRoom
{
    public sealed class LobbyChatTests : ViewModelTestBase<LobbyChatViewModel>
    {
        private ILobbyService _service;
        private IDialogScreenService _dialog;
        private LobbyState _lobby;

        protected override LobbyChatViewModel CreateViewModel()
        {
            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby");
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState>
            {
                ["player"] = Player("old", "earlier")
            };

            _service = Substitute.For<ILobbyService>();
            _service.CurrentLobbyState.Returns(_lobby);
            _service.SendMessageAsync(Arg.Any<string>()).Returns(Task.FromResult(Result.Success()));
            _dialog = Substitute.For<IDialogScreenService>();

            return new LobbyChatViewModel(_service, Substitute.For<ILocalizationDatabase>(), _dialog);
        }

        private static LobbyPlayerState Player(string id, string text)
        {
            return new LobbyPlayerState
            {
                UserId = "player",
                Data = new Dictionary<string, string>
                {
                    ["username"] = "Player",
                    ["LastMessage"] = JsonConvert.SerializeObject(new LobbyChatMessage.Payload { Id = id, Text = text })
                }
            };
        }

        private void Receive(string id, string text = "hello")
        {
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState> { ["player"] = Player(id, text) };
        }

        [Test]
        public void InitialLastMessage_IsIgnored()
        {
            Assert.That(ViewModel.Messages.Value, Is.Empty);
            Receive("old", "earlier");
            Assert.That(ViewModel.Messages.Value, Is.Empty);
        }

        [Test]
        public void RepeatedTextWithDifferentIds_IsDisplayedTwice()
        {
            Receive("first");
            Receive("second");
            Receive("second");
            Receive("first");

            Assert.That(ViewModel.Messages.Value.Count, Is.EqualTo(2));
            Assert.That(ViewModel.Messages.Value[0].Content, Is.EqualTo("hello"));
            Assert.That(ViewModel.Messages.Value[1].Username, Is.EqualTo("Player"));
        }

        [Test]
        public void TwoClients_ReceiveTheSameMemberUpdate()
        {
            using var other = new LobbyChatViewModel(_service, Substitute.For<ILocalizationDatabase>(), _dialog);
            Receive("first");

            Assert.That(ViewModel.Messages.Value.Count, Is.EqualTo(1));
            Assert.That(other.Messages.Value.Count, Is.EqualTo(1));
            Assert.That(other.Messages.Value[0].Id, Is.EqualTo(ViewModel.Messages.Value[0].Id));
        }

        [TestCase("not json")]
        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"Id\":\"id\",\"Text\":\" \"}")]
        public void InvalidMemberData_IsIgnored(string json)
        {
            LobbyPlayerState player = Player("id", "hello");
            player.Data["LastMessage"] = json;
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState> { ["player"] = player };

            Assert.That(ViewModel.Messages.Value, Is.Empty);
        }

        [Test]
        public void NewMember_DoesNotReplayAnExistingMessage()
        {
            LobbyPlayerState player = Player("new", "earlier");
            player.UserId = "new-player";
            _lobby.Players.Value = new Dictionary<string, LobbyPlayerState> { [player.UserId] = player };

            Assert.That(ViewModel.Messages.Value, Is.Empty);
        }

        [TestCase(0, false)]
        [TestCase(500, true)]
        [TestCase(501, false)]
        public void DraftLength_ControlsSending(int length, bool expected)
        {
            ViewModel.Draft.Value = new string('a', length);
            Assert.That(ViewModel.CanSend.Value, Is.EqualTo(expected));
        }

        [Test]
        public async Task Sending_UsesLobbyServiceAndBlocksConcurrentSubmission()
        {
            var completion = new TaskCompletionSource<ResultType>();
            _service.SendMessageAsync("hello").Returns(completion.Task);
            ViewModel.Draft.Value = "hello";

            Task sending = ViewModel.SendAsync();
            ViewModel.Draft.Value = "next";
            await ViewModel.SendAsync();
            Assert.That(ViewModel.CanSend.Value, Is.False);

            completion.SetResult(Result.Success());
            await sending;
            await _service.Received(1).SendMessageAsync("hello");
            await _service.DidNotReceive().SendMessageAsync("next");
            Assert.That(ViewModel.Draft.Value, Is.EqualTo("next"));
            Assert.That(ViewModel.CanSend.Value, Is.True);
        }

        [Test]
        public async Task FailedSend_RestoresDraftAndShowsError()
        {
            _service.SendMessageAsync("hello").Returns(Task.FromResult(Result.Failure("rate-limited")));
            ViewModel.Draft.Value = "hello";
            await ViewModel.SendAsync();

            Assert.That(ViewModel.Draft.Value, Is.EqualTo("hello"));
            Assert.That(ViewModel.CanSend.Value, Is.True);
            _dialog.Received().ShowErrorFromCode("error.lobby", "rate-limited");
        }

        [Test]
        public async Task FailedSend_PreservesTextTypedDuringRequest()
        {
            var completion = new TaskCompletionSource<ResultType>();
            _service.SendMessageAsync("hello").Returns(completion.Task);
            ViewModel.Draft.Value = "hello";
            Task sending = ViewModel.SendAsync();
            ViewModel.Draft.Value = "next";

            completion.SetResult(Result.Failure("failed-to-update"));
            await sending;
            Assert.That(ViewModel.Draft.Value, Is.EqualTo("hello\nnext"));
        }

        [Test]
        public async Task LobbyChanges_ClearMessagesAndIgnorePendingSend()
        {
            Receive("first");
            var completion = new TaskCompletionSource<ResultType>();
            _service.SendMessageAsync("hello").Returns(completion.Task);
            ViewModel.Draft.Value = "hello";
            Task sending = ViewModel.SendAsync();

            _lobby.Id.Value = new LobbyId("other");
            Receive("baseline");
            completion.SetResult(Result.Failure("failed-to-update"));
            await sending;

            Assert.That(ViewModel.Messages.Value, Is.Empty);
            Assert.That(ViewModel.Draft.Value, Is.Empty);
            _dialog.DidNotReceive().ShowErrorFromCode(Arg.Any<string>(), Arg.Any<string>());

            Receive("new");
            Assert.That(ViewModel.Messages.Value.Count, Is.EqualTo(1));
            _lobby.Id.Value = default;
            Assert.That(ViewModel.Messages.Value, Is.Empty);
            Assert.That(ViewModel.CanSend.Value, Is.False);
        }

        [Test]
        public async Task Disposal_UnsubscribesAndIgnoresPendingSend()
        {
            var completion = new TaskCompletionSource<ResultType>();
            _service.SendMessageAsync("hello").Returns(completion.Task);
            ViewModel.Draft.Value = "hello";
            Task sending = ViewModel.SendAsync();
            ViewModel.Dispose();

            Receive("late");
            completion.SetResult(Result.Failure("failed-to-update"));
            await sending;

            Assert.That(ViewModel.Messages.Value, Is.Empty);
            Assert.That(ViewModel.Draft.Value, Is.Empty);
            Assert.That(ViewModel.CanSend.Value, Is.False);
            _dialog.DidNotReceive().ShowErrorFromCode(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
