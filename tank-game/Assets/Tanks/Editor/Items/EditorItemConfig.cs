using FMODUnity;
using Tanks.Shared.Items.Data;
using UnityEngine;

namespace Tanks.Editor.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Items/Item")]
    public sealed class EditorItemConfig : ScriptableObject
    {
        [Header("Main Info")]
        [SerializeField] private string _identifier;
        [SerializeField] private ItemTier _tier;
        [SerializeField] private ItemStat[] _stats;
        [SerializeField] private ItemActionConfigBase[] _onPickUpActions;

        [Header("Visuals")]
        [SerializeField] private int _frameCount = 1;
        [SerializeField] private Material _iconMaterial;
        [SerializeField] private Material _glowMaterial;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _accentColor = Color.white;

        [Header("Audio")]
        [SerializeField] private EventReference _onSpawnSound;
        [SerializeField] private EventReference _onPickUpSound;

        public string Identifier => _identifier;
        public ItemTier Tier => _tier;
        public ItemStat[] Stats => _stats;
        public ItemActionConfigBase[] OnPickUpActions => _onPickUpActions;

        public int FrameCount => _frameCount;
        public Material IconMaterial => _iconMaterial;
        public Material GlowMaterial => _glowMaterial;
        public Sprite Icon => _icon;
        public Color AccentColor => _accentColor;

        public EventReference OnSpawnSound => _onSpawnSound;
        public EventReference OnPickUpSound => _onPickUpSound;
    }
}
