using Tanks.Core;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerConnector : NetworkComponent<IGameStartBehaviour>
    {
        public PlayerConnector(
            IClientConnectionValidator validator,
            IConnectedClientService clientService)
        {
            _validator = validator;
            _clientService = clientService;
        }

        private readonly IClientConnectionValidator _validator;
        private readonly IConnectedClientService _clientService;
        private bool _allPlayersConnected;

        protected override void OnInitialize()
        {
            if (NetworkManager.Singleton == false) return;

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        protected override void OnDestroy()
        {
            if (NetworkManager.Singleton == false) return;

            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        public void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;

            if (_allPlayersConnected)
            {
                response.Reason = "session-already-started";
                response.Approved = false;
                return;
            }

            ResultType<ClientConnectionPayload> result = _validator.Validate(request.Payload);

            if (result.IsSuccess)
            {
                ClientData data = new ClientData(result.Value.DisplayName);
                _clientService.AddClient(request.ClientNetworkId, data);

                response.Approved = true;
                return;
            }

            response.Reason = result.ErrorCode;
            response.Approved = false;
        }

        public void OnClientConnected(ulong clientId)
        {
            int currentCount = _clientService.ConnectedClientCount;
            if (currentCount < Behaviour.DesiredPlayerCount) return;

            _allPlayersConnected = true;
            Behaviour.PrepareGame();
        }

        public void OnClientDisconnected(ulong clientId)
        {
            _clientService.RemoveClient(clientId);
        }
    }
}
