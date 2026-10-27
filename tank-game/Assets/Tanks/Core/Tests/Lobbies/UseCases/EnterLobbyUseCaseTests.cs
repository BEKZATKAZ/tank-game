using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.UseCases;

namespace Tanks.Core.Tests.Lobbies.UseCases
{
    public sealed class EnterLobbyUseCaseTests
    {
        private ILobbyService _service;
        private ILobbyChatService _chat;
        private EnterLobbyUseCase _useCase;
        private LobbyState _lobby;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<ILobbyService>();
            _chat = Substitute.For<ILobbyChatService>();

            _lobby = new LobbyState();
            _lobby.Id.Value = new LobbyId("lobby");

            var entered = Task.FromResult(Result.Success(_lobby));
            _service.HostLobbyAsync(Arg.Any<string>(), Arg.Any<uint>()).Returns(entered);
            _service.JoinLobbyAsync(Arg.Any<LobbyId>()).Returns(entered);
            _service.JoinLobbyAsync(Arg.Any<LobbyAddress>()).Returns(entered);
            _service.LeaveLobbyAsync().Returns(Task.FromResult(Result.Success()));

            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Success()));
            _chat.DisconnectAsync().Returns(Task.FromResult(Result.Success()));

            _useCase = new EnterLobbyUseCase(_service, _chat);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public async Task Enter_WaitsForChatBeforeReturningLobby(int route)
        {
            var connected = new TaskCompletionSource<ResultType>();
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(connected.Task);

            Task<ResultType<LobbyState>> entering = Enter(route).AsTask();

            Assert.That(entering.IsCompleted, Is.False);

            connected.SetResult(Result.Success());
            ResultType<LobbyState> result = await entering;

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.SameAs(_lobby));
            await _service.DidNotReceive().LeaveLobbyAsync();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public async Task Enter_ChatFailure_WaitsForRollback(int route)
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));
            var left = new TaskCompletionSource<ResultType>();
            _service.LeaveLobbyAsync().Returns(left.Task);

            Task<ResultType<LobbyState>> entering = Enter(route).AsTask();

            Assert.That(entering.IsCompleted, Is.False);
            await _chat.Received(1).DisconnectAsync();

            left.SetResult(Result.Success());
            ResultType<LobbyState> result = await entering;

            Assert.That(result.ErrorCode, Is.EqualTo("chat-unavailable"));
            Assert.That(result.IsSuccess, Is.False);
            await _service.Received(1).LeaveLobbyAsync();
        }

        [Test]
        public async Task Join_LobbyFailure_DoesNotConnectOrLeave()
        {
            _service.JoinLobbyAsync(Arg.Any<LobbyId>())
                .Returns(Task.FromResult(Result.Failure<LobbyState>("failed-to-join")));

            ResultType<LobbyState> result = await _useCase.JoinAsync(_lobby.Id.Value);

            Assert.That(result.ErrorCode, Is.EqualTo("failed-to-join"));
            await _chat.DidNotReceive().ConnectAsync(Arg.Any<LobbyId>());
            await _service.DidNotReceive().LeaveLobbyAsync();
        }

        [Test]
        public async Task ChatDisconnectException_StillLeavesLobby()
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));
            _chat.DisconnectAsync().Returns(Task.FromException<ResultType>(new Exception("disconnect failed")));

            ResultType<LobbyState> result = await _useCase.JoinAsync(_lobby.Id.Value);

            Assert.That(result.IsSuccess, Is.False);
            await _service.Received(1).LeaveLobbyAsync();
        }

        [Test]
        public async Task RollbackFailure_ReportsLeaveError()
        {
            _chat.ConnectAsync(Arg.Any<LobbyId>()).Returns(Task.FromResult(Result.Failure("chat-unavailable")));
            _service.LeaveLobbyAsync().Returns(Task.FromResult(Result.Failure("failed-to-leave")));

            ResultType<LobbyState> result = await _useCase.HostAsync("lobby", 4);

            Assert.That(result.ErrorCode, Is.EqualTo("failed-to-leave"));
            Assert.That(result.IsSuccess, Is.False);
        }

        private UniTask<ResultType<LobbyState>> Enter(int route)
        {
            return route switch
            {
                0 => _useCase.HostAsync("lobby", 4),
                1 => _useCase.JoinAsync(_lobby.Id.Value),
                _ => _useCase.JoinAsync(new LobbyAddress("code"))
            };
        }
    }
}
