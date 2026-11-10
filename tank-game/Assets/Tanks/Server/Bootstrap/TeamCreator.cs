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
                ClientData data = _clientService.GetClientData(playerId);
                string displayName = data.DisplayName;
                TeamId id = new TeamId((ushort)(playerId + 100));

                _teamService.CreateTeamEveryoneRpc(id);
                _teamService.GetTeam(id).AddPlayer(playerId, displayName, data.Equipped);

                _teamService.AddPlayerClientRpc(new AddPlayerPayload
                {
                    BelongsTo = id,
                    OwnerPlayerId = playerId,
                    DisplayName = displayName,
                    Body = data.Equipped.Body ?? "v3-body",
                    Turret = data.Equipped.Turret ?? "v3-turret"
                });
            }
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
