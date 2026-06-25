using NaughtyAttributes;
using System;
using Tanks.Shared.Generation.Mazes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tanks.Shared.Generation
{
    [RequireComponent(typeof(Tilemap), typeof(Grid), typeof(TilemapCollider2D))]
    public sealed class TilemapSpawner : NetworkBehaviourExtended
    {
        [Required]
        [SerializeField]
        private TilemapThemeConfig _config;

        [SerializeField]
        private Vector2Int _resolution = new Vector2Int(12, 8);

        [Range(1, 5)]
        [SerializeField]
        private int _wallWidth = 1;

        private readonly IMazeGenerator _mazeGenerator = new DFSGenerator();
        private readonly ITilemapGenerator _tilemapGenerator = new TilemapGenerator();

        private void Start()
        {
            uint seed = (uint)Environment.TickCount;
            int2 resolution = new int2(_resolution.x, _resolution.y);

            using var cells = _mazeGenerator.Generate(seed, resolution);
            using var tiles = _tilemapGenerator.Generate(cells.Value, new TilemapGenerationSettings
            {
                Seed = seed,
                TileSpacing = 1,
                WallWidth = _wallWidth
            });

            Tilemap tilemap = GetComponent<Tilemap>();

            foreach (ref GeneratedTile tile in tiles.Value.AsSpan())
            {
                Tile unityTile = ScriptableObject.CreateInstance<Tile>();
                unityTile.sprite = _config.GetTileSpriteFromDirections(tile.Directions).Sprite;
                tilemap.SetTile(new Vector3Int(tile.Position.x, tile.Position.y), unityTile);
            }
        }
    }
}