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
        private Tile _backgroundTileAsset;

        public Tilemap Tilemap { get; private set; }

        public Bounds Spawn(TilemapGenerationSettings settings, Vector2Int resolution)
        {
            int2 newResolution = Unsafe.As<Vector2Int, int2>(ref resolution);

            using var cells = _mazeGenerator.Generate(settings.Seed, newResolution);
            using var tiles = _tilemapGenerator.Generate(cells.Value, settings);

            int2 gridResolution = newResolution * (settings.WallWidth * 2);

            FillBackground(gridResolution, Tilemap);
            FillForeground(tiles.Value.AsSpan(), Tilemap);

            // Should end at the very top right corner
            // Not at the very last tile origin
            gridResolution += settings.WallWidth;

            float3 size = new float3(gridResolution.x, gridResolution.y, 0);
            float3 center = size * 0.5f;

            return new Bounds(center, size + math.forward());
        }

        private void FillBackground(int2 gridResolution, Tilemap tilemap)
        {
            TileSprite sprite = _config.GetBackground();

            if (sprite is null || sprite.Sprite == false) return;

            if (_backgroundTileAsset == false)
            {
                _backgroundTileAsset = ScriptableObject.CreateInstance<Tile>();
                _backgroundTileAsset.sprite = sprite.Sprite;
                _backgroundTileAsset.color = sprite.Tint;
                _backgroundTileAsset.colliderType = Tile.ColliderType.None;
            }

            for (int y = 1; y < gridResolution.y; y++)
            {
                for (int x = 1; x < gridResolution.x; x++)
                {
                    Vector3Int position = new Vector3Int(x, y, -1);
                    tilemap.SetTile(position, _backgroundTileAsset);
                }
            }
        }

        private void FillForeground(Span<GeneratedTile> tiles, Tilemap tilemap)
        {
            foreach (ref GeneratedTile tile in tiles)
            {
                if (_cachedTileAssets.TryGetValue(tile.Directions, out Tile unityTile) == false)
                {
                    TileSprite sprite = _config.GetTileSpriteFromDirections(tile.Directions);
                    unityTile = ScriptableObject.CreateInstance<Tile>();
                    unityTile.sprite = sprite.Sprite;
                    unityTile.color = sprite.Tint;
                }

                Vector3Int position = new Vector3Int(tile.Position.x, tile.Position.y);
                tilemap.SetTile(position, unityTile);
            }
        }

        private void Awake()
        {
            Tilemap = GetComponent<Tilemap>();
        }

        private void OnDestroy()
        {
            foreach (Tile tile in _cachedTileAssets.Values) Destroy(tile);
            Destroy(_backgroundTileAsset);
            _cachedTileAssets.Clear();
        }
    }
}