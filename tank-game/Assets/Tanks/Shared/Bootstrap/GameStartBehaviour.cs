using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Bootstrap
{
	public struct WaitingForPlayersRequested { }
	public struct PrepareGameRequested { }

    public struct TilemapGenerated
	{
		public TilemapGenerationSettings Settings;
		public Bounds Bounds;
	}

	public struct PlayerReadyConfirmed
	{
		public int SecretHandle;
    }

    public struct PlayerReadyRequested
    {
        public int SecretHandle;
    }

    public struct GameStarted : INetworkSerializeByMemcpy
    {
        public float StartDelaySeconds;
    }

	public sealed class GameStartBehaviour : NetworkBehaviourExtended, IMessageSender
	{
		[SerializeField] private TilemapSpawner _tilemap;

		[Inject] private readonly IMessageBus _bus;

		[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
		public void GenerateTilemapEveryoneRpc(
			TilemapGenerationSettings settings, Vector2Int resolution)
		{
			int2 newResolution = new int2(resolution.x, resolution.y);

			_tilemap.Clear(settings, newResolution);
			Bounds bounds = _tilemap.Spawn(settings, newResolution);

			EmitMessage(new TilemapGenerated
			{
				Bounds = bounds,
				Settings = settings
			});
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

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
		public void StartGameClientRpc(GameStarted message)
		{
            EmitMessage(message);
            _bus.Publish(message, this);
        }

		public void PrepareGame() => EmitMessage(new PrepareGameRequested());
		public void WaitForPlayers() => EmitMessage(new WaitingForPlayersRequested());
	}
}
