using NaughtyAttributes;
using Tanks.Shared.Items;
using UnityEngine;

namespace Tanks.Client.Items
{
    public interface IClientItemConfig
    {
        ItemIdentifier Identifier { get; }

        int FrameCount { get; }
        Material IconMaterial { get; }
        Material GlowMaterial { get; }
        Sprite Icon { get; }

        string OnSpawnSound { get; }
        string OnPickUpSound { get; }
        Color AccentColor { get; }
    }

    public class ClientItemConfig : ScriptableObject, IClientItemConfig
    {
        [InfoBox(
            "This scriptable object is automatically generated " +
            "and should be managed from an EditorItemConfig asset.",
            type: EInfoBoxType.Warning
        )]

        [ReadOnly]
        [SerializeField]
        private string _identifier;

        [ReadOnly]
        [SerializeField]
        private int _frameCount;

        [ReadOnly]
        [SerializeField]
        private Material _iconMaterial;

        [ReadOnly]
        [SerializeField]
        private Material _glowMaterial;

        [ReadOnly]
        [SerializeField]
        private Sprite _icon;

        [ReadOnly]
        [SerializeField]
        private string _onPickUpSound;

        [ReadOnly]
        [SerializeField]
        private string _onSpawnSound;

        [ReadOnly]
        [SerializeField]
        private Color _accentColor;

        public ItemIdentifier Identifier => new ItemIdentifier(_identifier);

        public int FrameCount => _frameCount;
        public Material IconMaterial => _iconMaterial;
        public Material GlowMaterial => _glowMaterial;
        public Sprite Icon => _icon;
        public string OnSpawnSound => _onSpawnSound;
        public string OnPickUpSound => _onPickUpSound;
        public Color AccentColor => _accentColor;

#if UNITY_EDITOR
        public void InitializeFromEditor(
            string id)
        {
            _identifier = id;
        }

        public void InitializeVisualsFromEditor(
            int frameCount, Material iconMaterial, Material glowMaterial,
            Sprite icon, Color accentColor)
        {
            _frameCount = frameCount;
            _iconMaterial = iconMaterial;
            _glowMaterial = glowMaterial;
            _icon = icon;
            _accentColor = accentColor;
        }

        public void InitializeAudioFromEditor(string onSpawnSound, string onPickUpSound)
        {
            _onSpawnSound = onSpawnSound;
            _onPickUpSound = onPickUpSound;
        }
#endif
    }
}
