using NaughtyAttributes;
using System;
using System.Collections.Generic;
using Tanks.Shared.Generation.Mazes;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;

namespace Tanks.Shared.Generation
{
    public struct TilemapGenerated
    {
        public TilemapGenerationSettings Settings;
        public int2 Resolution;
        public Bounds Bounds;
    }

    [RequireComponent(typeof(Tilemap), typeof(Grid), typeof(TilemapCollider2D))]
    public sealed class TilemapSpawner : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private TilemapThemeConfig _config;

        [Required]
        [SerializeField]
        private SpriteRenderer _backgroundRenderer;

        [Inject] private readonly IMazeGenerator _mazeGenerator;
        [Inject] private readonly ITilemapGenerator _tilemapGenerator;

        private readonly Dictionary<DirectionFlags, Tile> _cachedTileAssets = new();

        public Tilemap Tilemap { get; private set; }

        public TemporaryArrayResult<GeneratedTile> Spawn(
            TilemapGenerationSettings settings, int2 resolution, out Bounds bounds)
        {
            using var cells = _mazeGenerator.Generate(settings.Seed, resolution);
            var tiles = _tilemapGenerator.Generate(cells.Value, settings, resolution);

            // Should end at the very top right corner
            // Not at the very last tile origin
            int2 gridResolution = resolution * (settings.WallWidth * 2);
            gridResolution += settings.WallWidth;

            float3 size = new float3(gridResolution.x, gridResolution.y, 0);
            float3 center = size * 0.5f;

            FillBackground(center);
            FillForeground(tiles.Value.AsSpan());
            GetComponent<TilemapCollider2D>().ProcessTilemapChanges();

            bounds = new Bounds(center, size + math.forward());
            return tiles;
        }

        public void Clear(TilemapGenerationSettings settings, int2 resolution)
        {
            _backgroundRenderer.sprite = null;

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

        private void FillBackground(float3 center)
        {
            TileSprite sprite = _config.GetBackground();
            if (sprite is null || sprite.Sprite == false) return;

            _backgroundRenderer.transform.position = center;
            _backgroundRenderer.sprite = sprite.Sprite;
            _backgroundRenderer.color = sprite.Tint;
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
            _cachedTileAssets.Clear();
        }
    }
}
