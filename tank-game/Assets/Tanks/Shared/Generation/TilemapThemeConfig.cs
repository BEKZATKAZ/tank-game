using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tanks.Shared.Generation
{
    [Serializable]
    public class TileSprite
    {
        [Required]
        [SerializeField]
        private Sprite _sprite;

        [SerializeField]
        private DirectionFlags _directions;

        [SerializeField]
        private Color _tint = Color.white;

        public Sprite Sprite => _sprite;

        public DirectionFlags Directions => (int)_directions is -1
            ? (DirectionFlags)0b1111 // Include all
            : _directions;

        public Color Tint => _tint;
    }

    [CreateAssetMenu(fileName = "TilemapTheme", menuName = "Scriptable Objects/Tilemap Theme")]
    public sealed class TilemapThemeConfig : ScriptableObject
    {
        [SerializeField] private TileSprite _background;
        [SerializeField] private TileSprite[] _parts;

        private readonly Dictionary<DirectionFlags, TileSprite> _cached = new();

        public TileSprite GetBackground() => _background;

        public TileSprite GetTileSpriteFromDirections(DirectionFlags directions)
        {
            if (_cached.TryGetValue(directions, out TileSprite foundPart)) return foundPart;
            foundPart = _parts.FirstOrDefault(x => x.Directions == directions);

            _cached.Add(directions, foundPart);
            if (foundPart.Sprite) return foundPart;

            Debug.LogWarningFormat("No tile sprite found for '{0}' direction(s)", directions);
            return _parts[0];
        }

        private void OnDestroy() => _cached.Clear();

#if UNITY_EDITOR
        private readonly HashSet<DirectionFlags> _directions = new();

        private void OnValidate()
        {
            _directions.Clear();

            foreach (TileSprite part in _parts)
            {
                if (_directions.Add(part.Directions)) continue;
                Debug.LogErrorFormat("Direction '{0}' is duplicated", part.Directions);
            }
        }
#endif
    }
}
