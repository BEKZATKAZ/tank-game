using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using Tanks.Core.Lobbies.UseCases;
using UnityEngine;

namespace Tanks.Core.Tests.Lobbies.UseCases
{
    public sealed class StartGameUseCaseTests
    {
        private IClientCommunicationService _client;
        private ILobbyService _lobbyService;
        private ISessionSettingsService _settings;
        private PrepareServerUseCase _useCase;

        [SetUp]
        public void SetUp()
        {
            _client = Substitute.For<IClientCommunicationService>();
            _lobbyService = Substitute.For<ILobbyService>();
            _settings = Substitute.For<ISessionSettingsService>();

            var defaults = Substitute.For<ISessionDefaultSettingsConfig>();
            defaults.DesiredPlayerCount.Returns(new PossibleIntegers(1, 2, 3, 4));
            defaults.MaxScoreToReach.Returns(new PossibleIntegers(5, 15, 30));
            defaults.SelfDamagePercent.Returns(new PossibleIntegers(5, 100, 200) { Default = 100 });
            defaults.Stats.Returns(new DefaultPlayerStats());
            defaults.MapGeneration.Returns(new DefaultMapGeneration());
            defaults.ItemSpawn.Returns(new DefaultItemSpawn());
            defaults.DangerZone.Returns(new DefaultDangerZone());
            _settings.DefaultSettings.Returns(defaults);

            _lobbyService.CurrentLobbyState.Returns(new LobbyState());
            _lobbyService.CurrentLobbyState.Players.Value.Add("player-1", default);
            _lobbyService.CurrentLobbyState.Players.Value.Add("player-2", default);
            _lobbyService.CurrentLobbyState.Players.Value.Add("player-3", default);

            _useCase = new PrepareServerUseCase(_client, _lobbyService, _settings);
        }

        [Test]
        public async Task StartAsync_WhenStartingLobbyUpdateFails_ReturnsError()
        {
            _lobbyService
                .UpdateLobbyAsync(
                    Arg.Is<LobbyUpdateRequest>(x => x.Status == LobbyStatus.PreparingServer))
                .Returns(Task.FromResult(Result.Failure<LobbyState>("lobby-error")));

            ResultType<string> result = await _useCase.PrepareAsync();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("lobby-error"));
        }

        [Test]
        public async Task StartAsync_WhenServerAlreadyProvidedJoinCode_UpdatesLobbyToConnecting()
        {
            RequestReturns("ABC123");

            _lobbyService
                .UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromResult(Result.Success<LobbyState>(null)));

            ResultType<string> result = await _useCase.PrepareAsync();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("ABC123"));

            await _lobbyService
                .Received(2)
                .UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());

            await _lobbyService
                .Received(1)
                .UpdateLobbyAsync(Arg.Is<LobbyUpdateRequest>(x =>
                    x.Status == LobbyStatus.PreparingServer));

            await _lobbyService
                .Received(1)
                .UpdateLobbyAsync(Arg.Is<LobbyUpdateRequest>(x =>
                    x.Status == LobbyStatus.Connecting &&
                    x.JoinCode == "ABC123"));
        }

        [Test]
        public async Task StartAsync_Success_ReturnsJoinCode()
        {
            RequestReturns("JOIN");

            _lobbyService
                .UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromResult(Result.Success<LobbyState>(null)));

            ResultType<string> result = await _useCase.PrepareAsync();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("JOIN"));

            _settings.Received(1).SetSettings(Arg.Any<SessionSettings>());
        }

        [Test]
        public async Task StartAsync_WhenConnectingLobbyUpdateFails_ReturnsError()
        {
            RequestReturns("ABC123");
            int callCount = 0;

            _lobbyService
                .UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(_ =>
                {
                    return ++callCount == 1
                        ? Task.FromResult(Result.Success<LobbyState>(null))
                        : Task.FromResult(Result.Failure<LobbyState>("connecting-error"));
                });

            ResultType<string> result = await _useCase.PrepareAsync();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("connecting-error"));
        }

        [Test]
        public async Task StartAsync_UsesPublishedSettingsAndActualPlayerCount()
        {
            var selected = SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings);
            selected.MaxScore = 30;
            selected.MapGeneration.Size = MapSize.Large;
            selected.SelfDamagePercent = 200;
            selected.MapGeneration.Thickness = MapThickness.Thick;
            selected.ItemSpawn.Rate = ItemSpawnRate.Chaos;
            selected.DangerZone.StartDelaySeconds = 60;
            selected.DangerZone.DurationSeconds = 50;
            selected.DangerZone.DamagePerSecond = 100;
            selected.PlayerCount = 8;
            var lobby = _lobbyService.CurrentLobbyState;
            lobby.LobbyData["settings"] = "published";
            lobby.Settings.Value = selected;
            _lobbyService.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromResult(Result.Success(lobby)));
            RequestReturns("JOIN");

            await _useCase.PrepareAsync();

            selected.PlayerCount = 3;
            await _client.Received(1).SendRequestAsync<PrepareServerRequest, ServerStandBy>(
                Arg.Is<PrepareServerRequest>(x => x.Settings.Equals(selected)), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task StartAsync_WaitsForPendingSaveBeforeLockingSettings()
        {
            var lobby = _lobbyService.CurrentLobbyState;
            await lobby.SettingsLock.WaitAsync();
            _lobbyService.UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>())
                .Returns(Task.FromResult(Result.Success(lobby)));
            RequestReturns("JOIN");

            Task<ResultType<string>> starting = _useCase.PrepareAsync().AsTask();
            Assert.That(lobby.Status.Value, Is.EqualTo(LobbyStatus.WaitingForPlayers));
            await _lobbyService.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());

            var selected = SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings);
            selected.MaxScore = 30;
            lobby.LobbyData["settings"] = "published";
            lobby.Settings.Value = selected;
            lobby.SettingsLock.Release();
            ResultType<string> result = await starting;

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(lobby.Status.Value, Is.EqualTo(LobbyStatus.PreparingServer));
            Assert.That(lobby.SettingsLock.CurrentCount, Is.EqualTo(1));
            await _client.Received(1).SendRequestAsync<PrepareServerRequest, ServerStandBy>(
                Arg.Is<PrepareServerRequest>(x => x.Settings.MaxScore == 30), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task StartAsync_LobbyChangesWhileWaiting_DoesNotPrepare()
        {
            var lobby = _lobbyService.CurrentLobbyState;
            await lobby.SettingsLock.WaitAsync();

            Task<ResultType<string>> starting = _useCase.PrepareAsync().AsTask();
            lobby.Id.Value = new LobbyId("next-lobby");
            lobby.SettingsLock.Release();
            ResultType<string> result = await starting;

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(lobby.SettingsLock.CurrentCount, Is.EqualTo(1));
            await _lobbyService.DidNotReceive().UpdateLobbyAsync(Arg.Any<LobbyUpdateRequest>());
        }

        private void RequestReturns(string joinCode)
        {
            int playerCount = _lobbyService.CurrentLobbyState.Players.Value.Count;

            _client
                .SendRequestAsync<PrepareServerRequest, ServerStandBy>(
                    Arg.Is<PrepareServerRequest>(x => x.Settings.PlayerCount == playerCount),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    Result.Success(new ServerStandBy { JoinCode = joinCode })));
        }
    }
}
