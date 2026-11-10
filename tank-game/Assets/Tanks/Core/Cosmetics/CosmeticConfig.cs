using Tanks.Core.Store;
using UnityEngine;

namespace Tanks.Core.Cosmetics
{
    [CreateAssetMenu(fileName = "Cosmetic", menuName = "Scriptable Objects/Store/Cosmetic")]
    public sealed class CosmeticConfig : PurchasableItem
    {
        [SerializeField] private CosmeticType _cosmeticType;
        [SerializeField] private Sprite _sprite;

        public CosmeticType CosmeticType => _cosmeticType;
        public Sprite Sprite => _sprite;
    }
}
