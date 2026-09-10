using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Bootstrap
{
	public struct WaitingForPlayersRequested { }
	public struct PrepareGameRequested { }

	public struct PlayerReadyConfirmed
	{
		public int SecretHandle;
    }

    public struct PlayerReadyRequested
    {
        public int SecretHandle;
    }

    public interface IGameStartBehaviour : INetworkBehaviourExtended
    {
        void GenerateTilemapEveryoneRpc(TilemapGenerationSettings settings, Vector2Int resolution);
        void SendReadyConfirmationServerRpc(int secretHandle);
        void RequestReadyConfirmation(ulong clientId, int secretHandle);
        void StartGameEveryoneRpc(GameStartedFromServer message);

        void SpawnPlayer(ulong clientId, float2 position, string displayName);
        void DespawnAllPlayers();

        void PrepareGame();
        void WaitForPlayers();
    }

	public sealed class GameStartBehaviour :
        NetworkBehaviourExtended, IGameStartBehaviour, IMessageSender
	{
		[SerializeField] private TilemapSpawner _tilemap;

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

        public void SpawnPlayer(ulong clientId, float2 position, string displayName)
        {
            Required<GameObject> playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

            GameObject player = Instantiate(
                playerPrefab.Value, new float3(position, 0), quaternion.identity);

            _container.InjectGameObject(player);

            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);

            IEntityBehaviour entity = player.GetComponent<IEntityBehaviour>();
            entity.SetDisplayNameEveryoneRpc(displayName);
        }

        public void PrepareGame() => EmitMessage(new PrepareGameRequested());
		public void WaitForPlayers() => EmitMessage(new WaitingForPlayersRequested());
    }
}
