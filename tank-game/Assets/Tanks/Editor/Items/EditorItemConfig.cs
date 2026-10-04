using FMODUnity;
using System.Collections.Generic;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using UnityEngine;

namespace Tanks.Editor.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Items/Item")]
    public sealed class EditorItemConfig : ScriptableObject
    {
        [Header("Main Info")]
        [SerializeField] private string _identifier;
        [SerializeField] private ItemTier _tier;
        [SerializeField] private Dictionary<StatType, PlayerStat> _playerStats;
        [SerializeField] private Dictionary<string, ItemStat> _itemStats;
        [SerializeField] private ItemActionConfigBase[] _onPickUpActions;

        [Header("Visuals")]
        [SerializeField] private Material _iconMaterial;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _accentColor = Color.white;

        [Header("Audio")]
        [SerializeField] private EventReference _onSpawnSound;
        [SerializeField] private EventReference _onPickUpSound;

        public string Identifier => _identifier;
        public ItemTier Tier => _tier;
        public Dictionary<StatType, PlayerStat> PlayerStats => _playerStats;
        public Dictionary<string, ItemStat> ItemStats => _itemStats;
        public ItemActionConfigBase[] OnPickUpActions => _onPickUpActions;

        public Material IconMaterial => _iconMaterial;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;

        public EventReference OnSpawnSound => _onSpawnSound;
        public EventReference OnPickUpSound => _onPickUpSound;
    }
}
