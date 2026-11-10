using UnityEngine;

namespace Tanks.Core.Store
{
    [CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Store/Item")]
    public class PurchasableItem : ScriptableObject
    {
        [SerializeField] private string _identifier;

        public string Identifier => _identifier;
    }
}
