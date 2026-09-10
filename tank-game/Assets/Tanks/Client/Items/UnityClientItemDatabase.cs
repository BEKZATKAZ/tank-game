using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Items;
using UnityEngine;

namespace Tanks.Client.Items
{
    public sealed class UnityClientItemDatabase : MonoBehaviour, IClientItemDatabase
    {
        [Required]
        [SerializeField]
        private ClientItemDatabaseConfig _database;

        private Dictionary<ItemIdentifier, IClientItemConfig> _cached;

        private void Awake()
        {
            _cached = _database.Items.ToDictionary(k => k.Identifier, v => v as IClientItemConfig);
        }

        public IClientItemConfig Get(ItemIdentifier id)
        {
            return _cached[id];
        }
    }
}
