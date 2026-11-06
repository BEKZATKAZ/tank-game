using Tanks.Client.Visuals;
using UnityEngine;
using System;
using Tanks.Shared.Effects;

namespace Tanks.Client.Entities
{
    [Serializable]
    public sealed class EffectVisualSettings
    {
        public EffectVisualSettings(EffectType type, string shaderProperty)
        {
            _type = type;
            _shaderProperty = shaderProperty;
        }

        [SerializeField] private EffectType _type;
        [SerializeField] private string _shaderProperty;
        [Min(0)] [SerializeField] private float _fadeInDuration = 0.3f;
        [Min(0)] [SerializeField] private float _fadeOutDuration = 0.6f;

        public EffectType Type => _type;
        public string ShaderProperty => _shaderProperty;
        public float FadeInDuration => _fadeInDuration;
        public float FadeOutDuration => _fadeOutDuration;
    }

    [CreateAssetMenu(fileName = "EntityVisuals", menuName = "Scriptable Objects/Entity Visuals")]
    public sealed class EntityVisualConfig : ScriptableObject
    {
        [Header("Hit")]
        [SerializeField] private VisualType _hitVisualType;
        [SerializeField] private string _hitSoundPath;

        [Header("Destroy")]
        [SerializeField] private VisualType _destroyVisualType;
        [SerializeField] private bool _allowDestroyPrediction = true;
        [SerializeField] private string _destroyBySelfSoundPath;
        [SerializeField] private string _destroyByOtherSoundPath;
        [SerializeField] private string _destroyByPlayerSoundPath;

        [Header("Effects")]
        [SerializeField] private EffectVisualSettings[] _effects =
        {
            new(EffectType.Burning, "_BurningIntensity"),
            new(EffectType.Frozen, "_FrozenIntensity"),
            new(EffectType.Healing, "_HealingIntensity"),
            new(EffectType.Noclip, "_NoclipIntensity")
        };

        public VisualType HitVisualType => _hitVisualType;
        public EffectVisualSettings[] Effects => _effects;
        public string HitSoundPath => _hitSoundPath;

        public VisualType DestoryVisualType => _destroyVisualType;
        public string DestroyBySelfSoundPath => _destroyBySelfSoundPath;
        public string DestroyByOtherSoundPath => _destroyByOtherSoundPath;
        public string DestroyByPlayerSoundPath => _destroyByPlayerSoundPath;
        public bool AllowDestroyPrediction => _allowDestroyPrediction;
    }
}
