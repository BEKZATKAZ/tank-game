using System.Collections.Generic;
using Tanks.Utils;

namespace Tanks.Shared.Teams
{
    public interface ITeamBehaviourService : INetworkBehaviourExtended
    {
        IReadOnlyBindableProperty<int> CurrentRound { get; }

        IEnumerable<Team> GetAllTeams();
        Team GetTeam(TeamId id);
        Team GetTeamByPlayerId(ulong clientId);
        bool TryGetTeamByPlayerId(ulong clientId, out Team result);

        void SetRoundEveryoneRpc(int value);
        void SyncPlayerState(PlayerState state);

        void AddPlayerClientRpc(AddPlayerPayload payload);
        void AddPlayerPointsClientRpc(UpdatePlayerPointsPayload payload);
        void AddTeamScoreEveryoneRpc(TeamId id, int amount);
        void CreateTeamEveryoneRpc(TeamId id);
    }
}
