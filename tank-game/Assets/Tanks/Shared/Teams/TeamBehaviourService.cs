using System.Collections.Generic;
using Tanks.Utils;
using Unity.Netcode;

namespace Tanks.Shared.Teams
{
    public sealed class TeamBehaviourService :
       NetworkBehaviourExtended, ITeamBehaviourService
    {
        public IReadOnlyBindableProperty<int> CurrentRound => _currentRound;

        private readonly Dictionary<TeamId, Team> _teams = new();
        private readonly BindableProperty<int> _currentRound = new();

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void SyncPlayerStateClientRpc(
            ulong playerId, int eliminations, int assistance, int deaths)
        {
            GetTeamByPlayerId(playerId).GetPlayer(playerId)
                .OverridePoints(eliminations, assistance, deaths);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SetRoundEveryoneRpc(int value)
        {
            _currentRound.Value = value;
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void AddPlayerClientRpc(AddPlayerPayload payload)
        {
            EmitMessage(payload);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void AddPlayerPointsClientRpc(UpdatePlayerPointsPayload payload)
        {
            EmitMessage(payload);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void AddTeamScoreEveryoneRpc(TeamId id, int amount)
        {
            GetTeam(id).AddScore(amount);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void CreateTeamEveryoneRpc(TeamId id)
        {
            _teams.Add(id, new Team(id));
        }

        public void SyncPlayerState(PlayerState state)
        {
            SyncPlayerStateClientRpc(
                state.PlayerId, state.Eliminations, state.Assistance, state.Deaths);
        }

        public IEnumerable<Team> GetAllTeams()
        {
            return _teams.Values;
        }

        public Team GetTeam(TeamId id)
        {
            return _teams[id];
        }

        public Team GetTeamByPlayerId(ulong clientId)
        {
            if (TryGetTeamByPlayerId(clientId, out Team team)) return team;

            throw new KeyNotFoundException(
                $"Player with ID '{clientId}' is not in any team");
        }

        public bool TryGetTeamByPlayerId(ulong clientId, out Team result)
        {
            foreach (Team team in _teams.Values)
            {
                foreach (var player in team.Players)
                {
                    if (player.PlayerId != clientId) continue;
                    result = team;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
