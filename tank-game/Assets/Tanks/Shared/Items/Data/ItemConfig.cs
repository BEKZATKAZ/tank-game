using NaughtyAttributes;
using System;
using System.Collections.Generic;
using Tanks.Shared.Stats;
using UnityEngine;

namespace Tanks.Shared.Items.Data
{
    [Serializable]
    public sealed class ItemStat
    {
        public string Name;
        public StatType Type;

        public float Value;
        public StatValueType ValueType;

        public float Duration = -1;
        public bool ApplyOnPickUp = true;
    }

    public interface IItemConfig
    {
        ItemIdentifier Identifier { get; }
        ItemTier Tier { get; }

        IEnumerable<ItemStat> Stats { get; }
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
        private ItemStat[] _stats;

        public ItemIdentifier Identifier => new ItemIdentifier(_identifier);
        public ItemTier Tier => _tier;

        public IEnumerable<ItemStat> Stats => _stats;
        public IEnumerable<ItemActionConfigBase> OnPickUpActions => _onPickUpActions;
        
        public ItemStat GetStat(string name)
        {
            for (int i = 0; i < _stats.Length; i++)
            {
                if (_stats[i].Name == name) return _stats[i];
            }

            throw new KeyNotFoundException($"Stat '{name}' was not found");
        }

#if UNITY_EDITOR
        public void InitializeFromEditor(string id, ItemTier tier, ItemStat[] stats)
        {
            _identifier = id;
            _tier = tier;
            _stats = stats;
        }

        public void InitializeActionsFromEditor(ItemActionConfigBase[] onPickUp)
        {
            _onPickUpActions = onPickUp;
        }
#endif
    }
}
