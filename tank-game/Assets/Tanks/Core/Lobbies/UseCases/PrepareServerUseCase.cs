using Cysharp.Threading.Tasks;
using System.Threading;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;

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
            // Prediction for host
            _lobbyService.CurrentLobbyState.Status.Value = LobbyStatus.PreparingServer;

            ResultType starting = await _lobbyService.UpdateLobbyAsync(new LobbyUpdateRequest
            {
                Status = LobbyStatus.PreparingServer
            });

            if (starting.IsSuccess == false)
            {
                return Result.Failure<string>(starting.ErrorCode);
            }

            var defaultSettings = _settings.DefaultSettings;
            var settings = SessionSettingsMapper.FromDefaultToLobby(defaultSettings);
            settings.PlayerCount = _lobbyService.CurrentLobbyState.Players.Value.Count;

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
