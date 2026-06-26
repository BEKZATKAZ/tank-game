using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tanks.Shared.Generation.Mazes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

namespace Tanks.Shared.Generation
{
    [RequireComponent(typeof(Tilemap), typeof(Grid), typeof(TilemapCollider2D))]
    public sealed class TilemapSpawner : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private TilemapThemeConfig _config;

        [Inject] private readonly IMazeGenerator _mazeGenerator;
        [Inject] private readonly ITilemapGenerator _tilemapGenerator;

        private readonly Dictionary<DirectionFlags, Tile> _cachedTileAssets = new();

        public void Spawn(TilemapGenerationSettings settings, Vector2Int resolution)
        {
            int2 newResolution = Unsafe.As<Vector2Int, int2>(ref resolution);

            using var cells = _mazeGenerator.Generate(settings.Seed, newResolution);
            using var tiles = _tilemapGenerator.Generate(cells.Value, settings);

            Tilemap tilemap = GetComponent<Tilemap>();

            foreach (ref GeneratedTile tile in tiles.Value.AsSpan())
            {
                if (_cachedTileAssets.TryGetValue(tile.Directions, out Tile unityTile) == false)
                {
                    unityTile = ScriptableObject.CreateInstance<Tile>();
                    unityTile.sprite = _config.GetTileSpriteFromDirections(tile.Directions).Sprite;
                }

                Vector3Int position = new Vector3Int(tile.Position.x, tile.Position.y);
                tilemap.SetTile(position, unityTile);
            }
        }

        private void OnDestroy()
        {
            foreach (Tile tile in _cachedTileAssets.Values) Destroy(tile);
            _cachedTileAssets.Clear();
        }
    }
}