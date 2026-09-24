using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Bootstrap;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientConnector : MonoBehaviour
    {
        [Inject] private readonly IAuthService _auth;
        [Inject] private readonly ISessionConnectionService _connection;
        [Inject] private readonly ISessionSettingsService _settings;
        [Inject] private readonly IClientCommunicationService _client;

        public async UniTask SetupRelayAsync()
        {
            string joinCode = _connection.JoinCode;
            NetworkManager manager = NetworkManager.Singleton;

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            if (Application.isPlaying == false) return;

            UnityTransport transport = manager.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        }

        private void SetupLocalhost()
        {
#if UNITY_EDITOR
            // Fire and forget
            _ = _client.SendRequestAsync<PrepareServerRequest, ServerStandBy>(
                new PrepareServerRequest
                {
                    IsLocalhost = true,
                    Settings = SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings)
                });

            SessionSettings defaultSettings = SessionSettingsMapper
                .FromLobby(default, _settings.DefaultSettings);

            _settings.SetSettings(defaultSettings);
#endif

            NetworkManager manager = NetworkManager.Singleton;
            UnityTransport oldTransport = manager.GetComponent<UnityTransport>();
            Destroy(oldTransport);

            UnityTransport newTransport = manager.gameObject.AddComponent<UnityTransport>();
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
            SceneManager.LoadScene(0);
        }

        private async void Start()
        {
            if (string.IsNullOrWhiteSpace(_connection.JoinCode)) SetupLocalhost();
            else await SetupRelayAsync();

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
