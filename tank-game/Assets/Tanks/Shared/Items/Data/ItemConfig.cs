using NaughtyAttributes;
using System;
using System.Collections.Generic;
using Tanks.Shared.Stats;
using UnityEngine;

namespace Tanks.Shared.Items.Data
{
    [Serializable]
    public sealed class PlayerStat
    {
        public float Duration = -1;
        public float Value;
        public StatValueType ValueType;
    }

    [Serializable]
    public struct ItemStat
    {
        public float Value;
        public StatValueType ValueType;
    }

    public interface IItemConfig
    {
        ItemIdentifier Identifier { get; }
        ItemTier Tier { get; }

        IReadOnlyDictionary<StatType, PlayerStat> PlayerStats { get; }
        IReadOnlyDictionary<string, ItemStat> ItemStats { get; }
        IEnumerable<ItemActionConfigBase> OnPickUpActions { get; }

        ItemStat GetStat(string name);
    }

    public sealed class ItemConfig : ScriptableObject, IItemConfig
    {
        [ReadOnly]
        [SerializeField]
        private string _identifier;

        [ReadOnly]
        [SerializeField]
        private ItemTier _tier;

        [ReadOnly]
        [SerializeField]
        private ItemActionConfigBase[] _onPickUpActions;

        [ReadOnly]
        [SerializeField]
        private Dictionary<StatType, PlayerStat> _playerStats;

        [ReadOnly]
        [SerializeField]
        private Dictionary<string, ItemStat> _itemStats;

        public ItemIdentifier Identifier => new ItemIdentifier(_identifier);
        public ItemTier Tier => _tier;

        public IReadOnlyDictionary<StatType, PlayerStat> PlayerStats => _playerStats;
        public IReadOnlyDictionary<string, ItemStat> ItemStats => _itemStats;
        public IEnumerable<ItemActionConfigBase> OnPickUpActions => _onPickUpActions;
        
        public ItemStat GetStat(string name)
        {
            if (_itemStats.TryGetValue(name, out ItemStat stat)) return stat;
            throw new KeyNotFoundException($"Stat '{name}' was not found");
        }

#if UNITY_EDITOR
        public void InitializeFromEditor(
            string id, ItemTier tier,
            Dictionary<StatType, PlayerStat> playerStats, Dictionary<string, ItemStat> stats)
        {
            _identifier = id;
            _tier = tier;
            _playerStats = playerStats;
            _itemStats = stats;
        }

        public void InitializeActionsFromEditor(ItemActionConfigBase[] onPickUp)
        {
            _onPickUpActions = onPickUp;
        }
#endif
    }
}
