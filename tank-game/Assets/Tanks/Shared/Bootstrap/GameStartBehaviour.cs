using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Helpers;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        int DesiredPlayerCount { get; }
        RpcTarget RpcTarget { get; }

        void GenerateTilemapEveryoneRpc(TilemapGenerationSettings settings, Vector2Int resolution);
        void SendReadyConfirmationServerRpc(int secretHandle);
        void RequestReadyConfirmationClientRpc(int secretHandle, RpcParams p = default);
        void StartGameEveryoneRpc(GameStarted message);

        int GetAlivePlayerCount();
        IEnumerable<ulong> GetConnectedClientIds();
        UniTask TryLoadSceneAsync();
        void SpawnPlayer(ulong clientId, float2 position);
        void DespawnAllPlayers();

        void PrepareGame();
        void WaitForPlayers();
    }

	public sealed class GameStartBehaviour :
        NetworkBehaviourExtended, IGameStartBehaviour, IMessageSender
	{
		[SerializeField] private TilemapSpawner _tilemap;
		[SerializeField] private int _desiredPlayerCount = 1;

		[SerializeField] private string _serverSceneName = "Gameplay_Server";
		[SerializeField] private string _clientSceneName = "Gameplay_Client";

        [Inject] private readonly IObjectResolver _container;
        [Inject] private readonly IMessageBus _bus;
		[Inject] private readonly IItemDatabase _itemDatabase;
        [Inject] private readonly LifetimeScope _scope;

		private bool _isSceneLoaded;

        public int DesiredPlayerCount => _desiredPlayerCount;

        protected override void OnInitialize()
        {
            _itemDatabase.TryInitializeAsync(IsServer);
        }

        public IEnumerable<ulong> GetConnectedClientIds()
        {
            return NetworkManager.ConnectedClientsIds;
        }

        public int GetAlivePlayerCount()
        {
            return NetworkManager.ConnectedClients.Count(x => x.Value.PlayerObject == true);
        }

        public async UniTask TryLoadSceneAsync()
		{
			if (_isSceneLoaded) return;

			string sceneName = IsServer ? _serverSceneName : _clientSceneName;
            _isSceneLoaded = true;

            using (LifetimeScope.EnqueueParent(_scope))
            {
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
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

        [Rpc(SendTo.SpecifiedInParams)]
        public void RequestReadyConfirmationClientRpc(int secretHandle, RpcParams p = default)
        {
            EmitMessage(new PlayerReadyRequested { SecretHandle = secretHandle });
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
		public void StartGameEveryoneRpc(GameStarted message)
		{
            EmitMessage(message);
            _bus.Publish(message, this);
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
            Required<GameObject> playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

            GameObject player = Instantiate(
                playerPrefab.Value, new float3(position, 0), quaternion.identity);

            _container.InjectGameObject(player);

            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        }

        public void PrepareGame() => EmitMessage(new PrepareGameRequested());
		public void WaitForPlayers() => EmitMessage(new WaitingForPlayersRequested());
    }
}
