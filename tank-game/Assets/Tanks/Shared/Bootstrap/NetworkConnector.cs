using System;
using Unity.Multiplayer;
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

            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;

            if (role is MultiplayerRoleFlags.Server)
            {
                manager.StartServer();
                return;
            }

            if (role is MultiplayerRoleFlags.Client)
            {
                manager.StartClient();
                return;
            }

            throw new NotImplementedException("Multiplayer role not supported");
        }
    }
}
