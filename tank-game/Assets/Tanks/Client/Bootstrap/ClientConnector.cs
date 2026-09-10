using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Tanks.Core;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Bootstrap;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientConnector : MonoBehaviour
    {
        [Inject] private readonly IAuthService _auth;
        [Inject] private readonly ISessionConnectionService _connection;

        private void Start()
        {
            NetworkManager manager = NetworkManager.Singleton;
            UnityTransport transport = manager.GetComponent<UnityTransport>();

            transport.SetConnectionData(_connection.IPAddress, _connection.Port);
            string displayName = _auth.CurrentProfile?.DisplayName?.Value;

            if (string.IsNullOrWhiteSpace(displayName) && Application.isEditor)
            {
                displayName = CurrentPlayer.Tags.FirstOrDefault() ?? "TestPlayer";
            }

            var payload = new ClientConnectionPayload { DisplayName = displayName };
            string json = JsonConvert.SerializeObject(payload);
            
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(json);
            manager.StartClient();
        }
    }
}
