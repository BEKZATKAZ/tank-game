using Tanks.Core;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Teams;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class PlayerConnector : NetworkComponent<IGameStartBehaviour>
    {
        public PlayerConnector(
            IClientConnectionValidator validator,
            IConnectedClientService clientService,
            IVirtualMachineService vmService,
            ISessionSettingsService sessionSettings,
            IServerLifetimeService lifetime,
            ITeamBehaviourService teamService)
        {
            _validator = validator;
            _clientService = clientService;
            _vmService = vmService;
            _sessionSettings = sessionSettings;
            _lifetime = lifetime;
            _teamService = teamService;
        }

        private readonly IClientConnectionValidator _validator;
        private readonly IConnectedClientService _clientService;
        private readonly IVirtualMachineService _vmService;
        private readonly ISessionSettingsService _sessionSettings;
        private readonly IServerLifetimeService _lifetime;
        private readonly ITeamBehaviourService _teamService;
        private bool _allPlayersConnected;

        protected override void OnInitialize()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            _vmService.StandBy();
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

            Debug.LogWarningFormat("Client rejected, reason: '{0}'", result.ErrorCode);
            response.Reason = result.ErrorCode;
            response.Approved = false;
        }

        public void OnClientConnected(ulong clientId)
        {
            int currentCount = _clientService.ConnectedClientCount;
            if (currentCount < _sessionSettings.GetSettings().DesiredPlayerCount) return;

            _allPlayersConnected = true;
            _lifetime.TrySetStep(GameStep.Session);

            Behaviour.PrepareGame();
        }

        public void OnClientDisconnected(ulong clientId)
        {
            _clientService.RemoveClient(clientId);

            if (_teamService.TryGetTeamByPlayerId(clientId, out Team team))
            {
                team.RemovePlayer(clientId);
            }

            if (_clientService.ConnectedClientCount > 0) return;

            Application.Quit();
        }
    }
}
