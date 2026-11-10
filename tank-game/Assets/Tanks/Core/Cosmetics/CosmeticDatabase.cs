using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Core.Cosmetics
{
    [CreateAssetMenu(
        fileName = "CosmeticDatabase",
        menuName = "Scriptable Objects/Store/Cosmetics Database")]
    public sealed class CosmeticDatabase : ScriptableObject
    {
        [SerializeField] private CosmeticConfig[] _cosmetics;

        public IReadOnlyList<CosmeticConfig> Cosmetics => _cosmetics;

        public CosmeticConfig Find(string identifier, CosmeticType type)
        {
            if (_cosmetics == null) return null;

            foreach (CosmeticConfig cosmetic in _cosmetics)
            {
                if (cosmetic && cosmetic.Identifier == identifier && cosmetic.CosmeticType == type) return cosmetic;
            }

            return null;
        }
    }
}
