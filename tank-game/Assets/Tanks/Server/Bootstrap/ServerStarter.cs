using Cysharp.Threading.Tasks;
using System;
using System.Threading;
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

        private async UniTask<string> SetupRelayAsync(int playerCount)
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

        private async UniTask AuthenticateAsync()
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

        private async UniTask<PrepareServerRequest> WaitForRequestAsync()
        {
            ResultType<PrepareServerRequest> start = await _server
                .ExpectRequestAsync<PrepareServerRequest>();

            if (start.IsSuccess) return start.Value;
            throw new Exception("Failed to receive start request");
        }

        private async void Start()
        {
            try
            {
                await _sharedItemDatabase.WaitForInitializationAsync();
                await AuthenticateAsync();

                // Stand by
                PrepareServerRequest request = await WaitForRequestAsync();

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
