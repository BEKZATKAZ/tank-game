using System.Collections.Generic;
using Tanks.Shared.Effects;
using UnityEngine;

namespace Tanks.Client.Effects
{
    [CreateAssetMenu(fileName = "EffectListConfig", menuName = "Scriptable Objects/Effect Config")]
    public sealed class EffectListConfig : ScriptableObject, IEffectListConfig
    {
        [SerializeField] private Dictionary<EffectType, EffectConfig> _effects;

        public IReadOnlyDictionary<EffectType, EffectConfig> Effects => _effects;
    }
}
