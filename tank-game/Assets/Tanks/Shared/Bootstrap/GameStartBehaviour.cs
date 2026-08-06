using Cysharp.Threading.Tasks;
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

	public sealed class GameStartBehaviour : NetworkBehaviourExtended, IMessageSender
	{
		[SerializeField] private TilemapSpawner _tilemap;
		[SerializeField] private int _desiredPlayerCount = 1;

		[SerializeField] private string _serverSceneName = "Gameplay_Server";
		[SerializeField] private string _clientSceneName = "Gameplay_Client";

		[Inject] private readonly IMessageBus _bus;
		[Inject] private readonly IItemDatabase _itemDatabase;
        [Inject] private readonly LifetimeScope _scope;

		private bool _isSceneLoaded;

        public int DesiredPlayerCount => _desiredPlayerCount;

        protected override void OnInitialize()
        {
            _itemDatabase.TryInitializeAsync(IsServer);
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

        public void PrepareGame() => EmitMessage(new PrepareGameRequested());
		public void WaitForPlayers() => EmitMessage(new WaitingForPlayersRequested());
	}
}
