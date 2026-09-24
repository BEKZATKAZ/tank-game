using NaughtyAttributes;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
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

        [Rpc(SendTo.SpecifiedInParams)]
        private void RequestReadyConfirmationClientRpc(int secretHandle, RpcParams p = default)
        {
            EmitMessage(new PlayerReadyRequested { SecretHandle = secretHandle });
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
		public void GenerateTilemapEveryoneRpc(
			TilemapGenerationSettings settings, Vector2Int resolution)
		{
			int2 newResolution = new int2(resolution.x, resolution.y);

			_tilemap.Clear(settings, newResolution);
			Bounds bounds = _tilemap.Spawn(settings, newResolution);

			TilemapGenerated message = new TilemapGenerated
			{
				Bounds = bounds,
				Settings = settings,
				Resolution = newResolution
			};

            EmitMessage(message);
			_bus.Publish(message, this);
        }

		[Rpc(SendTo.Server)]
		public void SendReadyConfirmationServerRpc(int secretHandle)
		{
            EmitMessage(new PlayerReadyConfirmed { SecretHandle = secretHandle });
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
		public void StartGameEveryoneRpc(GameStartedFromServer message)
        {
            EmitMessage(message);
            _bus.Publish(message, this);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void EndGameEveryoneRpc(TeamId winningTeamId)
        {
            EndGameFromServer message = new EndGameFromServer { WinningTeamId = winningTeamId };
            EmitMessage(message);
            _bus.Publish(message, this);
        }

        public void RequestReadyConfirmation(ulong clientId, int secretHandle)
        {
            RpcParams p = RpcTarget.Single(clientId, RpcTargetUse.Temp);
            RequestReadyConfirmationClientRpc(secretHandle, p);
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
