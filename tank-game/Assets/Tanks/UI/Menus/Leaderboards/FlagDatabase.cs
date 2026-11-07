using System.Collections.Generic;
using UnityEngine;

namespace Tanks.UI.Menus.Leaderboards
{
    [CreateAssetMenu(fileName = "FlagDatabase", menuName = "Scriptable Objects/Flag Database")]
    public sealed class FlagDatabase : ScriptableObject
    {
        [SerializeField] private Sprite[] _flags;

        private Dictionary<string, Sprite> _lookup;

        public Sprite GetFlag(string countryCode)
        {
            if (_lookup is null)
            {
                _lookup = new Dictionary<string, Sprite>();

                foreach (Sprite sprite in _flags)
                {
                    string code = sprite.name.Trim().ToUpperInvariant();
                    _lookup[code] = sprite;
                    Debug.Log(name);
                }
            }

            _lookup.TryGetValue((countryCode ?? "").Trim().ToUpperInvariant(), out Sprite flag);
            return flag;
        }
    }
}
