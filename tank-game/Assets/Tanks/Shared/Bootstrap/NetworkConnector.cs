using System;
using System.Linq;
using Unity.Multiplayer.PlayMode;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Bootstrap
{
    public sealed class NetworkConnector : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager manager = NetworkManager.Singleton;

            if (manager == false) return;

            if (CurrentPlayer.Tags.Any(x => x.Equals("server", StringComparison.OrdinalIgnoreCase)))
            {
                manager.StartServer();
                return;
            }

            manager.StartClient();
        }
    }
}
