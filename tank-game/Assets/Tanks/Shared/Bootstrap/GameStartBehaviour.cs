using Tanks.Shared.Generation;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Bootstrap
{
	public sealed class GameStartBehaviour : NetworkBehaviourExtended
	{
		[SerializeField] private TilemapSpawner _tilemap;

		[Rpc(SendTo.Everyone)]
		public void GenerateTilemapEveryoneRpc(
			TilemapGenerationSettings settings, Vector2Int resolution)
		{
			_tilemap.Spawn(settings, resolution);
        }
	}
}