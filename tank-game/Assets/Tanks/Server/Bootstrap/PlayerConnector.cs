using System.Linq;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerConnector : NetworkComponent<IGameStartBehaviour>
    {
        private bool _allPlayersConnected;

        protected override void OnInitialize()
        {
            if (NetworkManager.Singleton == false) return;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        protected override void OnDestroy()
        {
            if (NetworkManager.Singleton == false) return;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;

            // Reject if the game is starting or started
            response.Approved = _allPlayersConnected == false;
        }

        public void OnClientConnected(ulong clientId)
        {
            int currentCount = Behaviour.GetConnectedClientIds().Count();
            if (currentCount < Behaviour.DesiredPlayerCount) return;

            _allPlayersConnected = true;
            Behaviour.PrepareGame();
        }
    }
}
