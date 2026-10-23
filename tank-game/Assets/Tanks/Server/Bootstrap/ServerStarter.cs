using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Items;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Tanks.Server.Bootstrap
{
    public sealed class ServerStarter : MonoBehaviour
    {
        [Inject] private readonly IItemDatabase _sharedItemDatabase;
        [Inject] private readonly ISessionSettingsService _settings;
        [Inject] private readonly IServerCommunicationService _server;
        [Inject] private readonly IAuthService _auth;

        private IRelayService Relay => RelayService.Instance;

        private async Task<string> SetupRelayAsync(int playerCount)
        {
            Allocation allocation = await Relay.CreateAllocationAsync(playerCount);
            string joinCode = await Relay.GetJoinCodeAsync(allocation.AllocationId);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            return joinCode;
        }

        private string SetupLocalhost()
        {
            NetworkManager manager = NetworkManager.Singleton;

            UnityTransport oldTransport = manager.GetComponent<UnityTransport>();
            Destroy(oldTransport);

            UnityTransport newTransport = manager.gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = newTransport;

            return "";
        }

        private async Task AuthenticateAsync()
        {
            await _auth.InitializeAsync();

            if (Application.isEditor == false)
            {
                await _server.ExpectRequestAsync<AuthenticateServerRequest>();
                ResultType<string> code = await _auth.GenerateSignInCodeAsync("Server");

                await _server.RespondToRequestAsync(new ServerAuthReady { AuthCode = code.Value });
                await _auth.SignInWithCodeAsync();
                return;
            }

#if UNITY_EDITOR
            await _auth.SignInWithDeviceIdAsync();
#endif
        }

        private async Task<T> WaitForRequestAsync<T>()
        {
            ResultType<T> start = await _server.ExpectRequestAsync<T>();
            if (start.IsSuccess) return start.Value;
            throw new Exception("Failed to receive start request - " + start.ErrorCode);
        }

        private async void Start()
        {
            try
            {
                _server.Reinitialize();
                LoadSceneRequest sceneRequest = await WaitForRequestAsync<LoadSceneRequest>();

                if (sceneRequest.Scene is RequestedServerScene.Playground)
                {
                    // Remove current network manager so it doesn't replace new
                    Destroy(NetworkManager.Singleton.gameObject);

                    SceneManager.LoadScene("Playground");
                    return;
                }

                await _sharedItemDatabase.WaitForInitializationAsync();
                await AuthenticateAsync();

                // Stand by
                PrepareServerRequest request = await WaitForRequestAsync<PrepareServerRequest>();

                SessionSettings settings = SessionSettingsMapper
                    .FromLobby(request.Settings, _settings.DefaultSettings);

                _settings.SetSettings(settings);

                string joinCode = request.IsLocalhost
                    ? SetupLocalhost()
                    : await SetupRelayAsync(settings.DesiredPlayerCount);

                using CancellationTokenSource timeout = new CancellationTokenSource(10000);
                NetworkManager.Singleton.StartHost();

                await _server.RespondToRequestAsync(new ServerStandBy
                {
                    JoinCode = joinCode,
                    Settings = settings
                }, timeout.Token);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Application.Quit(1);
            }
        }
    }
}
