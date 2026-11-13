using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Client.Input.Glyphs
{
    [CreateAssetMenu(menuName = "Tanks/Input Glyph Database")]
    public sealed class InputGlyphDatabase : ScriptableObject
    {
        [SerializeField] private Dictionary<string, Sprite> _glyphs = new();

        public IReadOnlyDictionary<string, Sprite> Glyphs => _glyphs;

        public bool TryGet(string key, out Sprite sprite)
        {
            return _glyphs.TryGetValue(key ?? "", out sprite) && sprite != null;
        }

#if UNITY_EDITOR
        public void SetGlyphs(Dictionary<string, Sprite> glyphs) => _glyphs = glyphs;
#endif
    }
}
