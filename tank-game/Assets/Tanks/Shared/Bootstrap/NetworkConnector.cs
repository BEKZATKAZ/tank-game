using NaughtyAttributes;
using System;
using Unity.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.Shared.Bootstrap
{
    public sealed class NetworkConnector : MonoBehaviour
    {
        [Scene]
        [SerializeField]
        private string _clientScene = "Gameplay_Client";

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
                SceneManager.LoadSceneAsync(_clientScene, LoadSceneMode.Additive);
                return;
            }

            throw new NotImplementedException("Multiplayer role not supported");
        }
    }
}
