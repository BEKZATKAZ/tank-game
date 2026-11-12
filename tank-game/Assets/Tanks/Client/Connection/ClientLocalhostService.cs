using Cysharp.Threading.Tasks;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Tanks.Client.Connection
{
    public sealed class ClientLocalhostService
    {
        public ClientLocalhostService(
            IClientCommunicationService client,
            ISessionSettingsService settings)
        {
            _client = client;
            _settings = settings;
        }

        private readonly IClientCommunicationService _client;
        private readonly ISessionSettingsService _settings;

        public async UniTask SetupAsync()
        {
#if UNITY_EDITOR
            if (_client.IsConnected == false) _client.Reinitialize();

            if (CurrentPlayer.IsMainEditor)
            {
                await _client.SendRequestAsync(new LoadSceneRequest
                {
                    Scene = RequestedServerScene.Gameplay
                });

                // Fire and forget
                _ = _client.SendRequestAsync<PrepareServerRequest, ServerStandBy>(
                    new PrepareServerRequest
                    {
                        IsLocalhost = true,
                        Settings = SessionSettingsMapper.FromDefaultToLobby(
                            _settings.DefaultSettings)
                    });
            }

            SessionSettings defaultSettings = SessionSettingsMapper
                .FromLobby(default, _settings.DefaultSettings);

            _settings.SetSettings(defaultSettings);
#endif

            NetworkManager manager = NetworkManager.Singleton;

            UnityTransport oldTransport = manager.GetComponent<UnityTransport>();

            Object.Destroy(oldTransport);

            UnityTransport newTransport = manager.gameObject
                .AddComponent<UnityTransport>();

            newTransport.MaxPacketQueueSize = 1024;
            manager.NetworkConfig.NetworkTransport = newTransport;
        }

    }
}
