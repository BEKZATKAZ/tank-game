using Tanks.Shared.Generation;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Bootstrap
{
	public struct TilemapGenerated
	{
		public TilemapGenerationSettings Settings;
		public Bounds Bounds;
	}

	public sealed class GameStartBehaviour : NetworkBehaviourExtended
	{
		[SerializeField] private TilemapSpawner _tilemap;

		[Rpc(SendTo.Everyone)]
		public void GenerateTilemapEveryoneRpc(
			TilemapGenerationSettings settings, Vector2Int resolution)
		{
			Bounds bounds = _tilemap.Spawn(settings, resolution);

			EmitMessage(new TilemapGenerated
			{
				Bounds = bounds,
				Settings = settings
			});
        }
	}
}