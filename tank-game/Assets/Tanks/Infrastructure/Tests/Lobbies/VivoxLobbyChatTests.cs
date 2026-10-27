using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Infrastructure.Lobbies;
using Unity.Services.Vivox;

namespace Tanks.Infrastructure.Tests.Lobbies
{
    public sealed class VivoxLobbyChatTests
    {
        private IVivoxService _vivox;
        private VivoxLobbyChatService _service;
        private LobbyState _lobby;
        private readonly LobbyId _lobbyId = new("test");

        [SetUp]
        public void Setup()
        {
            _vivox = Substitute.For<IVivoxService>();
            _vivox.IsLoggedIn.Returns(true);

            _vivox.ActiveChannels.Returns(new ReadOnlyDictionary<string, ReadOnlyCollection<VivoxParticipant>>(
                new Dictionary<string, ReadOnlyCollection<VivoxParticipant>>
                {
                    ["lobby-test"] = new(Array.Empty<VivoxParticipant>()),
                    ["lobby-other"] = new(Array.Empty<VivoxParticipant>())
                }));

            _vivox.InitializeAsync().Returns(Task.CompletedTask);
            _vivox.When(service => service.Uninitialize()).Do(_ =>
                _vivox.InitializationState.Returns(VivoxInitializationState.Uninitialized));

            _vivox.JoinGroupChannelAsync(Arg.Any<string>(), ChatCapability.TextOnly).Returns(Task.CompletedTask);
            _vivox.LeaveChannelAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
            _vivox.SendChannelTextMessageAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);
            _vivox.GetChannelTextMessageHistoryAsync(Arg.Any<string>(), 50)
                .Returns(Task.FromResult(new ReadOnlyCollection<VivoxMessage>(Array.Empty<VivoxMessage>())));

            var auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(new PlayerProfileState());

            _lobby = new LobbyState();
            _lobby.Id.Value = _lobbyId;

            var lobbies = Substitute.For<ILobbyService>();
            lobbies.CurrentLobbyState.Returns(_lobby);

            _service = new VivoxLobbyChatService(auth, lobbies);

            typeof(VivoxLobbyChatService).GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, _vivox);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _service.DisconnectAsync();
            _service.Dispose();
        }

        [Test]
        public async Task Connect_MergesHistoryAndLiveMessagesInOrderWithoutDuplicates()
        {
            var history = new TaskCompletionSource<ReadOnlyCollection<VivoxMessage>>();
            _vivox.GetChannelTextMessageHistoryAsync("lobby-test", 50).Returns(history.Task);

            var received = new List<string>();
            _service.MessageReceived += message => received.Add(message.Id);

            VivoxMessage first = Message("first", 1);
            VivoxMessage second = Message("second", 2);
            VivoxMessage third = Message("third", 3);

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(second);
            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(third);

            Assert.That(received, Is.Empty);

            history.SetResult(new ReadOnlyCollection<VivoxMessage>(new[] { second, first }));
            ResultType result = await connecting;

            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(second);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(received, Is.EqualTo(new[] { "first", "second", "third" }));

            Assert.That(_service.Messages.Count, Is.EqualTo(3));
            Assert.That(_service.Messages[0].IsHistory, Is.True);
            Assert.That(_service.Messages[1].IsHistory, Is.False);
            Assert.That(_service.Messages[2].IsHistory, Is.False);
        }

        [Test]
        public async Task Send_TrimsMessageAndUsesCurrentLobby()
        {
            await _service.ConnectAsync(_lobbyId);

            ResultType result = await _service.SendAsync(_lobbyId, " hello ");

            Assert.That(result.IsSuccess, Is.True);
            await _vivox.Received(1).SendChannelTextMessageAsync("lobby-test", "hello");
        }

        [Test]
        public async Task Send_OtherLobby_IsRejected()
        {
            await _service.ConnectAsync(_lobbyId);

            ResultType result = await _service.SendAsync(new LobbyId("other"), "hello");

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.DidNotReceive().SendChannelTextMessageAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task Disconnect_LeavesChannelAndStopsMessages()
        {
            await _service.ConnectAsync(_lobbyId);

            int received = 0;
            _service.MessageReceived += _ => received++;

            await _service.DisconnectAsync();
            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(Message("late", 1));

            Assert.That(received, Is.Zero);
            Assert.That(_service.Messages, Is.Empty);
            await _vivox.Received(1).LeaveChannelAsync("lobby-test");
        }

        [Test]
        public async Task HistoryFailure_LeavesChannelAndReturnsFailure()
        {
            _vivox.GetChannelTextMessageHistoryAsync("lobby-test", 50)
                .Returns(Task.FromException<ReadOnlyCollection<VivoxMessage>>(new Exception("history unavailable")));

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.Received(1).LeaveChannelAsync("lobby-test");
        }

        [Test]
        public async Task SendFailure_ReturnsError()
        {
            await _service.ConnectAsync(_lobbyId);

            _vivox.SendChannelTextMessageAsync("lobby-test", "hello")
                .Returns(Task.FromException(new Exception("send unavailable")));

            ResultType result = await _service.SendAsync(_lobbyId, "hello");

            Assert.That(result.ErrorCode, Is.EqualTo("chat-send-failed"));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task Send_LostChannel_ReturnsFailureWithoutSending()
        {
            await _service.ConnectAsync(_lobbyId);

            _vivox.ActiveChannels.Returns(new ReadOnlyDictionary<string, ReadOnlyCollection<VivoxParticipant>>(
                new Dictionary<string, ReadOnlyCollection<VivoxParticipant>>()));

            ResultType result = await _service.SendAsync(_lobbyId, "hello");

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.DidNotReceive().SendChannelTextMessageAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task LobbySwitch_LeavesPreviousChannelAndIgnoresLateMessages()
        {
            await _service.ConnectAsync(_lobbyId);

            var received = new List<LobbyChatMessage>();
            _service.MessageReceived += received.Add;

            await _service.ConnectAsync(new LobbyId("other"));
            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(Message("late", 1));

            await _vivox.Received(1).LeaveChannelAsync("lobby-test");
            await _vivox.Received(1).JoinGroupChannelAsync("lobby-other", ChatCapability.TextOnly);
            Assert.That(received, Is.Empty);
        }

        [Test]
        public async Task Disconnect_DuringConnect_WaitsAndLeavesJoinedChannel()
        {
            var history = new TaskCompletionSource<ReadOnlyCollection<VivoxMessage>>();
            _vivox.GetChannelTextMessageHistoryAsync("lobby-test", 50).Returns(history.Task);

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            Task<ResultType> disconnecting = _service.DisconnectAsync();

            Assert.That(disconnecting.IsCompleted, Is.False);

            history.SetResult(new ReadOnlyCollection<VivoxMessage>(Array.Empty<VivoxMessage>()));
            await connecting;
            await disconnecting;

            await _vivox.Received(1).LeaveChannelAsync("lobby-test");
            Assert.That((await _service.SendAsync(_lobbyId, "hello")).IsSuccess, Is.False);
        }

        [Test]
        public async Task Dispose_DuringLogin_CancelsConnectionWithoutReportingFailure()
        {
            var login = new TaskCompletionSource<bool>();
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(login.Task);
            _vivox.InitializationState.Returns(VivoxInitializationState.Initialized);

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            _service.Dispose();
            ResultType result = await connecting;

            login.SetException(new OperationCanceledException("SDK uninitialized"));

            Assert.That(result.ErrorCode, Is.EqualTo("cancelled"));
            _vivox.Received(1).Uninitialize();
            await _vivox.DidNotReceive().JoinGroupChannelAsync(Arg.Any<string>(), ChatCapability.TextOnly);
        }

        [Test]
        public async Task LoginTimeout_ResetsSdkAndAllowsRetry()
        {
            var login = new TaskCompletionSource<bool>();
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(login.Task);
            _vivox.InitializationState.Returns(VivoxInitializationState.Initialized);

            typeof(VivoxLobbyChatService).GetField("_loginTimeout", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, TimeSpan.FromMilliseconds(25));

            ResultType failed = await _service.ConnectAsync(_lobbyId);
            login.SetException(new OperationCanceledException("SDK uninitialized"));

            _vivox.IsLoggedIn.Returns(true);
            ResultType retried = await _service.ConnectAsync(_lobbyId);

            Assert.That(failed.IsSuccess, Is.False);
            Assert.That(retried.IsSuccess, Is.True);
            _vivox.Received(1).Uninitialize();
        }

        [Test]
        public async Task LeavingLobby_DisconnectsChatAndClearsCachedMessages()
        {
            await _service.ConnectAsync(_lobbyId);

            _vivox.ChannelMessageReceived += Raise.Event<Action<VivoxMessage>>(Message("live", 1));

            _lobby.Id.Value = default;
            await _service.DisconnectAsync();

            Assert.That(_service.Messages, Is.Empty);
            await _vivox.Received(1).LeaveChannelAsync("lobby-test");
        }

        [Test]
        public async Task SlowLogin_UsesSeparateBudgetAndDoesNotResetConnector()
        {
            var login = new TaskCompletionSource<bool>();
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(login.Task);
            SetTiming("_operationTimeout", TimeSpan.FromMilliseconds(10));
            SetTiming("_loginTimeout", TimeSpan.FromSeconds(2));

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            await Task.Delay(40);

            Assert.That(connecting.IsCompleted, Is.False);

            _vivox.IsLoggedIn.Returns(true);
            login.SetResult(true);
            ResultType result = await connecting;

            Assert.That(result.IsSuccess, Is.True);
            _vivox.DidNotReceive().Uninitialize();
        }

        [Test]
        public async Task SdkLoginTimeout_RetriesOnceWithoutDestroyingConnector()
        {
            int attempts = 0;
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(_ =>
            {
                attempts++;
                if (attempts == 1) return Task.FromException(new TimeoutException("SDK login timeout"));

                _vivox.IsLoggedIn.Returns(true);
                return Task.CompletedTask;
            });
            SetTiming("_loginRetryDelay", TimeSpan.Zero);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.True);
            await _vivox.Received(2).LoginAsync(Arg.Any<LoginOptions>());
            await _vivox.Received(1).InitializeAsync();
            _vivox.DidNotReceive().Uninitialize();
        }

        [Test]
        public async Task MissingAuthenticationStanza_RetriesOnceWithoutDestroyingConnector()
        {
            int attempts = 0;
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(_ =>
            {
                attempts++;
                if (attempts == 1) return Task.FromException(VivoxFailure(5044));

                _vivox.IsLoggedIn.Returns(true);
                return Task.CompletedTask;
            });
            SetTiming("_loginRetryDelay", TimeSpan.Zero);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.True);
            await _vivox.Received(2).LoginAsync(Arg.Any<LoginOptions>());
            await _vivox.Received(1).JoinGroupChannelAsync("lobby-test", ChatCapability.TextOnly);
            _vivox.DidNotReceive().Uninitialize();
        }

        [Test]
        public async Task RepeatedMissingAuthenticationStanza_StopsAfterTwoAttempts()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(Task.FromException(VivoxFailure(5044)));
            SetTiming("_loginRetryDelay", TimeSpan.Zero);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.ErrorCode, Is.EqualTo("chat-unavailable"));
            await _vivox.Received(2).LoginAsync(Arg.Any<LoginOptions>());
            await _vivox.DidNotReceive().JoinGroupChannelAsync(Arg.Any<string>(), ChatCapability.TextOnly);
        }

        [TestCase(20121)]
        [TestCase(20122)]
        [TestCase(20128)]
        public async Task InvalidAuthentication_DoesNotRetry(int statusCode)
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(Task.FromException(VivoxFailure(statusCode)));
            SetTiming("_loginRetryDelay", TimeSpan.Zero);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.Received(1).LoginAsync(Arg.Any<LoginOptions>());
            await _vivox.DidNotReceive().JoinGroupChannelAsync(Arg.Any<string>(), ChatCapability.TextOnly);
        }

        [Test]
        public async Task ShutdownDuringHandshakeRetry_DoesNotAttemptLoginAgain()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(Task.FromException(VivoxFailure(5044)));

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            _service.Dispose();
            ResultType result = await connecting;

            Assert.That(result.ErrorCode, Is.EqualTo("cancelled"));
            await _vivox.Received(1).LoginAsync(Arg.Any<LoginOptions>());
        }

        [Test]
        public async Task RepeatedSdkTimeout_StopsAfterTwoAttempts()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>())
                .Returns(Task.FromException(new TimeoutException("SDK login timeout")));
            _vivox.InitializationState.Returns(VivoxInitializationState.Initialized);
            SetTiming("_loginRetryDelay", TimeSpan.Zero);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.Received(2).LoginAsync(Arg.Any<LoginOptions>());
            _vivox.Received(1).Uninitialize();
        }

        [Test]
        public async Task CompletedLoginWithoutSession_DoesNotJoinChannel()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>()).Returns(Task.CompletedTask);

            ResultType result = await _service.ConnectAsync(_lobbyId);

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.DidNotReceive().JoinGroupChannelAsync(Arg.Any<string>(), ChatCapability.TextOnly);
        }

        [Test]
        public async Task ShutdownDuringRetry_DoesNotAttemptLoginAgain()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>())
                .Returns(Task.FromException(new TimeoutException("SDK login timeout")));

            Task<ResultType> connecting = _service.ConnectAsync(_lobbyId);
            _service.Dispose();
            ResultType result = await connecting;

            Assert.That(result.ErrorCode, Is.EqualTo("cancelled"));
            await _vivox.Received(1).LoginAsync(Arg.Any<LoginOptions>());
        }

        [Test]
        public async Task LoginBudgetExpires_CancelsScheduledRetry()
        {
            _vivox.IsLoggedIn.Returns(false);
            _vivox.LoginAsync(Arg.Any<LoginOptions>())
                .Returns(Task.FromException(new TimeoutException("SDK login timeout")));
            SetTiming("_loginTimeout", TimeSpan.FromMilliseconds(20));
            SetTiming("_loginRetryDelay", TimeSpan.FromMilliseconds(100));

            ResultType result = await _service.ConnectAsync(_lobbyId);
            await Task.Delay(150);

            Assert.That(result.IsSuccess, Is.False);
            await _vivox.Received(1).LoginAsync(Arg.Any<LoginOptions>());
        }

        private static VivoxApiException VivoxFailure(int statusCode)
        {
            // The SDK constructor resolves error strings through its native library.
            var exception = (VivoxApiException)FormatterServices.GetUninitializedObject(typeof(VivoxApiException));
            typeof(VivoxApiException).GetProperty(nameof(VivoxApiException.StatusCode)).SetValue(exception, statusCode);
            return exception;
        }

        private void SetTiming(string name, TimeSpan value)
        {
            typeof(VivoxLobbyChatService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, value);
        }

        private static VivoxMessage Message(string id, int seconds)
        {
            var message = (VivoxMessage)FormatterServices.GetUninitializedObject(typeof(VivoxMessage));
            Set(nameof(VivoxMessage.MessageId), id);
            Set(nameof(VivoxMessage.ChannelName), "lobby-test");
            Set(nameof(VivoxMessage.SenderPlayerId), "player");
            Set(nameof(VivoxMessage.MessageText), "hello");
            Set(nameof(VivoxMessage.ReceivedTime), new DateTime(2026, 1, 1).AddSeconds(seconds));

            return message;

            void Set(string property, object value) => typeof(VivoxMessage).GetProperty(property).SetValue(message, value);
        }
    }
}
