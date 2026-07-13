using NaughtyAttributes;
using System;
using System.Collections.Generic;
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

        public Bounds Spawn(TilemapGenerationSettings settings, int2 resolution)
        {
            using var cells = _mazeGenerator.Generate(settings.Seed, resolution);
            using var tiles = _tilemapGenerator.Generate(cells.Value, settings);

            int2 gridResolution = resolution * (settings.WallWidth * 2);

            FillBackground(gridResolution);
            FillForeground(tiles.Value.AsSpan());

            // Should end at the very top right corner
            // Not at the very last tile origin
            gridResolution += settings.WallWidth;

            float3 size = new float3(gridResolution.x, gridResolution.y, 0);
            float3 center = size * 0.5f;

            return new Bounds(center, size + math.forward());
        }

        public void Clear(TilemapGenerationSettings settings, int2 resolution)
        {
            int2 gridResolution = resolution * (settings.WallWidth * 2);

            for (int y = 0; y < gridResolution.y; y++)
            {
                for (int x = 0; x < gridResolution.x; x++)
                {
                    // Foreground
                    Tilemap.SetTile(new Vector3Int(x, y), null);

                    // Background
                    Tilemap.SetTile(new Vector3Int(x, y, -1), null);
                }
            }
        }

        private void FillBackground(int2 gridResolution)
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
                    Tilemap.SetTile(position, _backgroundTileAsset);
                }
            }
        }

        private void FillForeground(Span<GeneratedTile> tiles)
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
                Tilemap.SetTile(position, unityTile);
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
