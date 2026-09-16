using VContainer;

namespace Tanks.Shared.Teams
{
    public sealed class TeamParticipantBehaviour :
        NetworkBehaviourExtended, ITeamParticipantBehaviour
    {
        [Inject] private readonly ITeamBehaviourService _service;

        public Team BelongsTo
        {
            get
            {
                if (_team != null) return _team;
                _service?.TryGetTeamByPlayerId(OwnerClientId, out _team);
                return _team;
            }
        }

        public PlayerState State
        {
            get
            {
                _state ??= BelongsTo?.GetPlayer(OwnerClientId);
                return _state;
            }
        }

        private Team _team;
        private PlayerState _state;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            State?.SetEliminated(false);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            State?.SetEliminated(true);
        }
    }
}
