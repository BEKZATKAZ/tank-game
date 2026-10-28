using Cysharp.Threading.Tasks;
using System.Threading;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IPrepareServerUseCase
    {
        UniTask<ResultType<string>> PrepareAsync();
    }

    public sealed class PrepareServerUseCase : IPrepareServerUseCase
    {
        public PrepareServerUseCase(
            IClientCommunicationService client,
            ILobbyService lobbyService,
            ISessionSettingsService settings)
        {
            _client = client;
            _lobbyService = lobbyService;
            _settings = settings;
        }

        private readonly IClientCommunicationService _client;
        private readonly ILobbyService _lobbyService;
        private readonly ISessionSettingsService _settings;

        public async UniTask<ResultType<string>> PrepareAsync()
        {
            LobbyState lobby = _lobbyService.CurrentLobbyState;
            LobbyId lobbyId = lobby.Id.Value;
            SessionSettingsFromLobby settings;
            await lobby.SettingsLock.WaitAsync();

            try
            {
                if (lobby.Id.Value.Value != lobbyId.Value ||
                    lobby.Status.Value != LobbyStatus.WaitingForPlayers)
                {
                    return Result.Failure<string>("failed-to-update");
                }

                // Lock editing before publishing the preparing status.
                lobby.Status.Value = LobbyStatus.PreparingServer;

                ResultType starting = await _lobbyService.UpdateLobbyAsync(new LobbyUpdateRequest
                {
                    Status = LobbyStatus.PreparingServer
                });

                if (starting.IsSuccess == false)
                {
                    return Result.Failure<string>(starting.ErrorCode);
                }

                if (lobby.Id.Value.Value != lobbyId.Value)
                {
                    return Result.Failure<string>("failed-to-update");
                }

                settings = lobby.LobbyData.ContainsKey("settings")
                    ? lobby.Settings.Value
                    : SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings);
                settings.PlayerCount = lobby.Players.Value.Count;
            }
            finally
            {
                lobby.SettingsLock.Release();
            }

            using CancellationTokenSource timeout = new CancellationTokenSource(15000);

            ResultType<ServerStandBy> standBy = await _client
                .SendRequestAsync<PrepareServerRequest, ServerStandBy>(new PrepareServerRequest
                {
                    Settings = settings
                }, timeout.Token);

            if (standBy.IsSuccess == false) return Result.Failure<string>(standBy.ErrorCode);
            string joinCode = standBy.Value.JoinCode;

            ResultType started = await _lobbyService.UpdateLobbyAsync(new LobbyUpdateRequest
            {
                JoinCode = joinCode,
                Status = LobbyStatus.Connecting,
                ServerConfirmedSettings = standBy.Value.Settings
            });

            if (started.IsSuccess)
            {
                _settings.SetSettings(standBy.Value.Settings);
                return joinCode;
            }

            return Result.Failure<string>(started.ErrorCode);
        }
    }
}
