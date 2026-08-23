using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerConnector : NetworkComponent<GameStartBehaviour>
    {
        private bool _allPlayersConnected;

        protected override void OnInitialize()
        {
            Behaviour.NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            Behaviour.NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        protected override void OnDestroy()
        {
            if (Behaviour.NetworkManager == false) return;
            Behaviour.NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            Behaviour.NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;

            // Reject if the game is starting or started
            response.Approved = _allPlayersConnected == false;
        }

        private void OnClientConnected(ulong clientId)
        {
            int currentCount = Behaviour.NetworkManager.ConnectedClients.Count;
            if (currentCount < Behaviour.DesiredPlayerCount) return;

            _allPlayersConnected = true;
            Behaviour.PrepareGame();
        }
    }
}
