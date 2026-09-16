using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Teams;

namespace Tanks.Server.Bootstrap
{
    public sealed class TeamCreator :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<PrepareGameRequested>
    {
        public TeamCreator(
            ITeamBehaviourService service,
            IConnectedClientService clientService)
        {
            _teamService = service;
            _clientService = clientService;
        }

        private readonly ITeamBehaviourService _teamService;
        private readonly IConnectedClientService _clientService;
        private bool _isInitialized;

        public void Handle(PrepareGameRequested message)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            foreach (ulong playerId in _clientService.GetConnectedClientIds())
            {
                string displayName = _clientService.GetClientData(playerId).DisplayName;
                TeamId id = new TeamId((ushort)(playerId + 100));

                _teamService.CreateTeamEveryoneRpc(id);
                _teamService.GetTeam(id).AddPlayer(playerId, displayName);

                _teamService.AddPlayerClientRpc(new AddPlayerPayload
                {
                    BelongsTo = id,
                    OwnerPlayerId = playerId,
                    DisplayName = displayName
                });
            }
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
