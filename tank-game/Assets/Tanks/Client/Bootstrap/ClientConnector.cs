using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Tanks.Shared.Bootstrap;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientConnector : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager manager = NetworkManager.Singleton;
            string displayName = CurrentPlayer.Tags.FirstOrDefault() ?? "EditorPlayer";

            var payload = new ClientConnectionPayload { DisplayName = displayName };
            string json = JsonConvert.SerializeObject(payload);

            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(json);
            manager.StartClient();
        }
    }
}
