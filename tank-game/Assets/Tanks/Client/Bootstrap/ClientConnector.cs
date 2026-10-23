using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Tanks.Client.Items;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Items;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientConnector : MonoBehaviour
    {
        [Inject] private readonly IAuthService _auth;
        [Inject] private readonly ISessionSettingsService _settings;
        [Inject] private readonly IClientCommunicationService _client;
        [Inject] private readonly ISceneLoader _sceneLoader;
        [Inject] private readonly IItemDatabase _sharedItemDatabase;
        [Inject] private readonly IClientItemDatabase _clientItemDatabase;

        public async UniTask SetupRelayAsync(LoadGameplay data)
        {
            _settings.SetSettings(data.Settings);

            NetworkManager manager = NetworkManager.Singleton;
            JoinAllocation allocation = await RelayService.Instance
                .JoinAllocationAsync(data.JoinCode);

            if (Application.isPlaying == false) return;

            UnityTransport transport = manager.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        }

        private async UniTask SetupLocalhost()
        {
#if UNITY_EDITOR
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
                        Settings = SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings)
                    });
            }

            SessionSettings defaultSettings = SessionSettingsMapper
                .FromLobby(default, _settings.DefaultSettings);

            _settings.SetSettings(defaultSettings);
#endif

            NetworkManager manager = NetworkManager.Singleton;
            UnityTransport oldTransport = manager.GetComponent<UnityTransport>();
            Destroy(oldTransport);

            UnityTransport newTransport = manager.gameObject.AddComponent<UnityTransport>();
            newTransport.MaxPacketQueueSize = 1024;
            manager.NetworkConfig.NetworkTransport = newTransport;
        }

        private string GetDisplayName()
        {
            string displayName = _auth.CurrentProfile?.DisplayName?.Value;

#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = CurrentPlayer.Tags.FirstOrDefault() ?? "TestPlayer";
            }
#endif

            return displayName;
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData e)
        {
            if (e.EventType != ConnectionEvent.ClientDisconnected) return;
            _sceneLoader.StartLoading(new LoadMenus(), withTips: false);
        }

        private UniTask InitializeItemsAsync()
        {
            UniTask shared = _sharedItemDatabase.WaitForInitializationAsync();
            UniTask client = _clientItemDatabase.WaitForInitializationAsync();
            return UniTask.WhenAll(shared, client);
        }

        private async void Start()
        {
#if UNITY_EDITOR
            if (_sceneLoader.IsLoading == false)
            {
                _sceneLoader.StartLoading(new LoadGameplay(), loadActualScene: false);
            }

            _client.Reinitialize();
#endif

            await InitializeItemsAsync();
            _sceneLoader.Progress = 0.6f;

            LoadGameplay data = _sceneLoader.GetLoadingParameters<LoadGameplay>();

            if (string.IsNullOrWhiteSpace(data.JoinCode)) await SetupLocalhost();
            else await SetupRelayAsync(data);

            _sceneLoader.Progress = 0.7f;

            string json = JsonConvert.SerializeObject(new ClientConnectionPayload
            {
                DisplayName = GetDisplayName()
            });

            NetworkManager manager = NetworkManager.Singleton;
            manager.OnConnectionEvent += OnConnectionEvent;

            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(json);
            manager.StartClient();
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == false) return;
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }
}
