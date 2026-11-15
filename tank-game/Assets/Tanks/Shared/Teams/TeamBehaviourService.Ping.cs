using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Teams
{
    public sealed partial class TeamBehaviourService
    {
        private float _nextPingUpdate;

        protected override void Update()
        {
            base.Update();

            if (IsSpawned == false || IsServer == false) return;
            if (Time.unscaledTime < _nextPingUpdate) return;

            _nextPingUpdate = Time.unscaledTime + 1;

            foreach (Team team in _teams.Values)
            {
                foreach (PlayerState player in team.Players)
                {
                    if (NetworkManager.ConnectedClients.ContainsKey(
                        player.PlayerId) == false) continue;

                    NetworkTransport transport = NetworkManager.NetworkConfig
                        .NetworkTransport;

                    ulong ping = player.PlayerId == Unity.Netcode.NetworkManager
                        .ServerClientId ? 0 : transport.GetCurrentRtt(
                            player.PlayerId);

                    player.SetPing(ping);
                    SyncPlayerPingClientRpc(player.PlayerId, ping);
                }
            }
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void SyncPlayerPingClientRpc(ulong playerId, ulong milliseconds)
        {
            if (TryGetTeamByPlayerId(playerId, out Team team) == false) return;

            team.GetPlayer(playerId).SetPing(milliseconds);
        }
    }
}
