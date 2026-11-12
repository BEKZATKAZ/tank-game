using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Tanks.Client.Connection;
using Tanks.Client.Items;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Players;
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
        [Inject] private readonly ILocalPlayerService _player;
        [Inject] private readonly ILobbyService _lobbyService;
        [Inject] private readonly ISessionSettingsService _settings;
        [Inject] private readonly IClientCommunicationService _client;
        [Inject] private readonly ISceneLoader _sceneLoader;
        [Inject] private readonly IItemDatabase _sharedItemDatabase;
        [Inject] private readonly IClientItemDatabase _clientItemDatabase;
        [Inject] private readonly GameplaySessionService _session;
        [Inject] private readonly ClientConnectionRecoveryService _recovery;
        [Inject] private readonly ClientLocalhostService _localhost;

        private bool _connected;

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

        private string GetDisplayName()
        {
            string displayName = _session.IsDirectSceneTest ? null :
                _auth.CurrentProfile?.DisplayName?.Value;

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
            if (e.EventType == ConnectionEvent.ClientConnected)
            {
                _connected = true;
                return;
            }

            if (e.EventType != ConnectionEvent.ClientDisconnected) return;

            string reason = string.IsNullOrWhiteSpace(manager.DisconnectReason)
                ? "connection-failed" : manager.DisconnectReason;

            if (_connected == false) Debug.LogWarning(
                "Connection failed: " + reason);

            _recovery.ReturnToMenus(_connected ? null : reason);
        }

        private UniTask InitializeItemsAsync()
        {
            UniTask shared = _sharedItemDatabase.WaitForInitializationAsync();
            UniTask client = _clientItemDatabase.WaitForInitializationAsync();
            return UniTask.WhenAll(shared, client);
        }

        private async void Start()
        {
            bool directSceneTest = false;
#if UNITY_EDITOR
            if (_sceneLoader.IsLoading == false)
            {
                directSceneTest = true;
                _sceneLoader.StartLoading(
                    new LoadGameplay(), loadActualScene: false);
            }

            _client.Reinitialize();
#endif

            _session.Begin(directSceneTest,
                directSceneTest ? null : _auth.CurrentProfile?.UserId.Value);

            if (directSceneTest == false && string.IsNullOrWhiteSpace(
                _lobbyService.CurrentLobbyState.Id.Value.Value) == false)
            {
                _ = _lobbyService.LeaveLobbyAsync();
            }

            await InitializeItemsAsync();
            _sceneLoader.Progress = 0.6f;

            LoadGameplay data = _sceneLoader.GetLoadingParameters<LoadGameplay>();

            UniTask setup = string.IsNullOrWhiteSpace(data.JoinCode)
                ? _localhost.SetupAsync() : SetupRelayAsync(data);

            await setup;

            _sceneLoader.Progress = 0.7f;

            string json = JsonConvert.SerializeObject(new ClientConnectionPayload
            {
                PlayerId = _session.PlayerId,
                IsDirectSceneTest = _session.IsDirectSceneTest,
                DisplayName = GetDisplayName(),
                Equipped = _player.LocalPlayer.Equipped.Value
            });

            NetworkManager manager = NetworkManager.Singleton;
            manager.OnConnectionEvent += OnConnectionEvent;

            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(json);

            if (manager.StartClient() == false)
            {
                _recovery.ReturnToMenus("connection-failed");
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == false) return;
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }
}
