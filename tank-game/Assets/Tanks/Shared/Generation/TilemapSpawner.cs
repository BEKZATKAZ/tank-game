using NaughtyAttributes;
using System;
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

        public void Spawn(TilemapGenerationSettings settings, Vector2Int resolution)
        {
            int2 newResolution = Unsafe.As<Vector2Int, int2>(ref resolution);

            using var cells = _mazeGenerator.Generate(settings.Seed, newResolution);
            using var tiles = _tilemapGenerator.Generate(cells.Value, settings);

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