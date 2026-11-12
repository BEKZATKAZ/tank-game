using NaughtyAttributes;
using System;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Utils;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Bootstrap
{
	public sealed class GameStartBehaviour :
        NetworkBehaviourExtended, IGameStartBehaviour, IMessageSender
	{
        [Required]
        [SerializeField]
        private NetworkObject _playerPrefab;

        [Required]
		[SerializeField]
        private TilemapSpawner _tilemap;

        [Inject] private readonly IObjectResolver _container;
        [Inject] private readonly IMessageBus _bus;
        private string _matchId;

        [Rpc(SendTo.SpecifiedInParams)]
        private void RequestReadyConfirmationClientRpc(
            int secretHandle,
            RpcParams p = default)
        {
            EmitMessage(new PlayerReadyRequested { SecretHandle = secretHandle });
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void GenerateTilemapClientRpc(
            TilemapGenerationSettings settings, Vector2Int resolution)
        {
            GenerateTilemap(settings, resolution).Dispose();
        }

		public TemporaryArrayResult<GeneratedTile> GenerateTilemap(
			TilemapGenerationSettings settings, Vector2Int resolution)
		{
			int2 newResolution = new int2(resolution.x, resolution.y);

			_tilemap.Clear(settings, newResolution);
			var tiles = _tilemap.Spawn(settings, newResolution, out Bounds bounds);

			TilemapGenerated message = new TilemapGenerated
			{
				Bounds = bounds,
				Settings = settings,
				Resolution = newResolution
			};

            EmitMessage(message);
			_bus.Publish(message, this);

            if (IsServer) GenerateTilemapClientRpc(settings, resolution);
            return tiles;
        }

		[Rpc(SendTo.Server)]
		public void SendReadyConfirmationServerRpc(int secretHandle)
		{
            EmitMessage(new PlayerReadyConfirmed { SecretHandle = secretHandle });
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
		public void StartGameEveryoneRpc(GameStartedFromServer message)
        {
            if (IsServer && _matchId == null) _matchId = Guid.NewGuid().ToString("N");
            EmitMessage(message);
            _bus.Publish(message, this);
        }

        public void EndGameEveryoneRpc(TeamId winningTeamId)
        {
            EndGameResultEveryoneRpc(winningTeamId, _matchId);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void EndGameResultEveryoneRpc(TeamId winningTeamId, string matchId)
        {
            EndGameFromServer message = new EndGameFromServer
            {
                WinningTeamId = winningTeamId,
                MatchId = matchId
            };
            EmitMessage(message);
            _bus.Publish(message, this);
        }

        public void RequestReadyConfirmation(ulong clientId, int secretHandle)
        {
            RpcParams p = RpcTarget.Single(clientId, RpcTargetUse.Temp);
            RequestReadyConfirmationClientRpc(secretHandle, p);
        }

        public void SendLeaderboardResult(ulong clientId, LeaderboardMatchResult result)
        {
            if (IsServer == false) return;
            LeaderboardResultClientRpc(result, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        public void SendDebriefing(ulong clientId, string matchId, string json)
        {
            DebriefingClientRpc(matchId, json, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void DebriefingClientRpc(
            string matchId,
            string json,
            RpcParams p = default)
        {
            EmitMessage(new DebriefingDelivered { MatchId = matchId, Json = json });
        }

        [Rpc(SendTo.Server)]
        public void AcknowledgeDebriefingServerRpc(
            string matchId,
            RpcParams rpcParams = default)
        {
            EmitMessage(new DebriefingAcknowledged
            {
                MatchId = matchId,
                ClientId = rpcParams.Receive.SenderClientId
            });
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void LeaderboardResultClientRpc(
            LeaderboardMatchResult result,
            RpcParams p = default)
        {
            EmitMessage(result);
        }

        public void DespawnAllPlayers()
        {
            foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
            {
                if (client.PlayerObject == false) continue;
                client.PlayerObject.Despawn(destroy: true);
            }
        }

        public void SpawnPlayer(ulong clientId, float2 position)
        {
            NetworkObject player = Instantiate(
                _playerPrefab, new float3(position, 0), quaternion.identity);

            _container.InjectGameObject(player.gameObject);
            player.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }

        public void PrepareGame() => EmitMessage(new PrepareGameRequested());
		public void WaitForPlayers() => EmitMessage(new WaitingForPlayersRequested());
    }
}
