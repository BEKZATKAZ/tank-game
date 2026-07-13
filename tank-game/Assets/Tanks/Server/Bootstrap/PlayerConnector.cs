using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerConnector : NetworkComponent<GameStartBehaviour>
    {
        private readonly int _desiredCount = 2; // ! Testing purposes
        private bool _allPlayersConnected;

        private void Start()
        {
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
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
            int currentCount = NetworkManager.ConnectedClients.Count;
            if (currentCount < _desiredCount) return;

            _allPlayersConnected = true;
            Behaviour.PrepareGame();
        }
    }
}
