using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Client.Items
{
    public sealed class ClientItemDatabaseConfig : ScriptableObject
    {
        [ReadOnly]
        [SerializeField]
        private List<ClientItemConfig> _items;

        public IEnumerable<ClientItemConfig> Items => _items;

#if UNITY_EDITOR
        public void AddFromEditor(ClientItemConfig item)
        {
            _items ??= new List<ClientItemConfig>();
            _items.Add(item);
        }
#endif
    }
}
