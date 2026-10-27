using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Infrastructure.Lobbies;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Tanks.Infrastructure.Tests.Lobbies
{
    public sealed class LobbyHeartbeatTests
    {
        private UGSLobbyService _service;
        private Unity.Services.Lobbies.ILobbyService _sdk;
        private ILobbyEvents _events;
        private PlayerProfileState _profile;
        private readonly Queue<TaskCompletionSource<bool>> _delays = new();
        private readonly List<TimeSpan> _intervals = new();
        private readonly List<CancellationToken> _tokens = new();

        [SetUp]
        public void Setup()
        {
            _delays.Clear();
            _intervals.Clear();
            _tokens.Clear();

            _profile = new PlayerProfileState();
            _profile.UserId.Value = "host";
            _profile.DisplayName.Value = "Host";

            var auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(_profile);

            _sdk = Substitute.For<Unity.Services.Lobbies.ILobbyService>();
            _events = Substitute.For<ILobbyEvents>();
            _events.UnsubscribeAsync().Returns(Task.CompletedTask);

            var lobby = new Lobby(id: "lobby", name: "Lobby", hostId: "host", maxPlayers: 4,
                players: new List<Player> { new("host"), new("guest") }, data: new Dictionary<string, DataObject>());

            _sdk.CreateLobbyAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CreateLobbyOptions>())
                .Returns(Task.FromResult(lobby));
            _sdk.JoinLobbyByIdAsync(Arg.Any<string>(), Arg.Any<JoinLobbyByIdOptions>())
                .Returns(Task.FromResult(lobby));
            _sdk.SubscribeToLobbyEventsAsync(Arg.Any<string>(), Arg.Any<LobbyEventCallbacks>())
                .Returns(Task.FromResult(_events));
            _sdk.SendHeartbeatPingAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
            _sdk.DeleteLobbyAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

            _service = new UGSLobbyService(auth);
            SetField("_service", _sdk);
            SetField("_heartbeatDelay", (Func<TimeSpan, CancellationToken, Task>)DelayAsync);
        }

        [TearDown]
        public async Task TearDown()
        {
            _service.Dispose();
            await HeartbeatTask;
        }

        private Task HeartbeatTask => (Task)typeof(UGSLobbyService)
            .GetField("_heartbeatTask", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_service);

        [Test]
        public async Task Host_PingsBeforeEventSubscriptionCompletes()
        {
            var subscribed = new TaskCompletionSource<ILobbyEvents>();
            _sdk.SubscribeToLobbyEventsAsync(Arg.Any<string>(), Arg.Any<LobbyEventCallbacks>()).Returns(subscribed.Task);

            Task<ResultType<LobbyState>> hosting = _service.HostLobbyAsync("Lobby", 4);

            Assert.That(hosting.IsCompleted, Is.False);
            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");

            subscribed.SetResult(_events);
            Assert.That((await hosting).IsSuccess, Is.True);
        }

        [Test]
        public async Task Heartbeat_Waits15SecondsAndStaysOnCallingThread()
        {
            int thread = Thread.CurrentThread.ManagedThreadId;
            var pingThreads = new List<int>();
            _sdk.SendHeartbeatPingAsync("lobby").Returns(_ =>
            {
                pingThreads.Add(Thread.CurrentThread.ManagedThreadId);
                return Task.CompletedTask;
            });

            await _service.HostLobbyAsync("Lobby", 4);

            Assert.That(pingThreads.Count, Is.EqualTo(1));
            Assert.That(_intervals[0], Is.EqualTo(TimeSpan.FromSeconds(15)));

            _delays.Dequeue().SetResult(true);

            Assert.That(pingThreads.Count, Is.EqualTo(2));
            Assert.That(pingThreads, Is.All.EqualTo(thread));
            Assert.That(_intervals, Is.All.EqualTo(TimeSpan.FromSeconds(15)));
        }

        [Test]
        public async Task FailedPing_RetriesAfterNextInterval()
        {
            _sdk.SendHeartbeatPingAsync("lobby")
                .Returns(Task.FromException(new Exception("temporary failure")), Task.CompletedTask);

            await _service.HostLobbyAsync("Lobby", 4);
            _delays.Dequeue().SetResult(true);

            await _sdk.Received(2).SendHeartbeatPingAsync("lobby");
            Assert.That(_intervals.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Leave_CancelsPendingDelay()
        {
            await _service.HostLobbyAsync("Lobby", 4);

            await _service.LeaveLobbyAsync();
            await HeartbeatTask;

            Assert.That(_tokens[0].IsCancellationRequested, Is.True);
            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
        }

        [Test]
        public async Task LobbyDeletion_CancelsPendingDelay()
        {
            await _service.HostLobbyAsync("Lobby", 4);

            typeof(UGSLobbyService).GetMethod("OnLobbyDeleted", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_service, null);
            await HeartbeatTask;

            Assert.That(_tokens[0].IsCancellationRequested, Is.True);
            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
        }

        [Test]
        public async Task Leave_StopsHeartbeatBeforeEventUnsubscriptionCompletes()
        {
            await _service.HostLobbyAsync("Lobby", 4);

            var unsubscribed = new TaskCompletionSource<bool>();
            _events.UnsubscribeAsync().Returns(unsubscribed.Task);

            Task<ResultType> leaving = _service.LeaveLobbyAsync();

            Assert.That(leaving.IsCompleted, Is.False);
            Assert.That(_tokens[0].IsCancellationRequested, Is.True);

            unsubscribed.SetResult(true);
            await leaving;
            await HeartbeatTask;
        }

        [Test]
        public async Task LobbyIdChange_StopsPreviousHeartbeat()
        {
            await _service.HostLobbyAsync("Lobby", 4);

            _service.CurrentLobbyState.Id.Value = new LobbyId("other");
            await HeartbeatTask;

            Assert.That(_tokens[0].IsCancellationRequested, Is.True);
            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
            await _sdk.DidNotReceive().SendHeartbeatPingAsync("other");
        }

        [Test]
        public async Task Dispose_DuringPing_DoesNotScheduleAnotherPing()
        {
            var ping = new TaskCompletionSource<bool>();
            _sdk.SendHeartbeatPingAsync("lobby").Returns(ping.Task);
            await _service.HostLobbyAsync("Lobby", 4);

            _service.Dispose();
            ping.SetResult(true);
            await HeartbeatTask;

            Assert.That(_intervals, Is.Empty);
            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
        }

        [Test]
        public async Task Guest_DoesNotSendHeartbeat()
        {
            _profile.UserId.Value = "guest";

            await _service.JoinLobbyAsync(new LobbyId("lobby"));

            await _sdk.DidNotReceive().SendHeartbeatPingAsync(Arg.Any<string>());
            Assert.That(_intervals, Is.Empty);
        }

        [Test]
        public async Task RepeatedStart_KeepsOneHeartbeatLoop()
        {
            await _service.HostLobbyAsync("Lobby", 4);
            await _service.HostLobbyAsync("Lobby", 4);

            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
            _delays.Dequeue().SetResult(true);

            await _sdk.Received(2).SendHeartbeatPingAsync("lobby");
        }

        [Test]
        public async Task LosingHostOwnership_StopsPinging()
        {
            await _service.HostLobbyAsync("Lobby", 4);

            _service.CurrentLobbyState.HostId.Value = "guest";
            _delays.Dequeue().SetResult(true);
            await HeartbeatTask;

            await _sdk.Received(1).SendHeartbeatPingAsync("lobby");
        }

        [Test]
        public async Task HostingAfterLeaving_StartsFreshLoop()
        {
            await _service.HostLobbyAsync("Lobby", 4);
            await _service.LeaveLobbyAsync();

            await _service.HostLobbyAsync("Lobby", 4);

            await _sdk.Received(2).SendHeartbeatPingAsync("lobby");
        }

        private Task DelayAsync(TimeSpan duration, CancellationToken cancellation)
        {
            var delay = new TaskCompletionSource<bool>();
            cancellation.Register(() => delay.TrySetCanceled());

            _intervals.Add(duration);
            _tokens.Add(cancellation);
            _delays.Enqueue(delay);
            return delay.Task;
        }

        private void SetField(string name, object value)
        {
            typeof(UGSLobbyService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, value);
        }
    }
}
