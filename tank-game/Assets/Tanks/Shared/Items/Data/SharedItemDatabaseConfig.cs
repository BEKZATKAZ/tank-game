using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Shared.Items.Data
{
    public sealed class SharedItemDatabaseConfig : ScriptableObject
    {
        [ReadOnly]
        [SerializeField]
        private List<ItemConfig> _items;

        public IEnumerable<ItemConfig> Items => _items;

#if UNITY_EDITOR
        public void AddFromEditor(ItemConfig item)
        {
            _items ??= new List<ItemConfig>();
            _items.Add(item);
        }
#endif
    }
}
