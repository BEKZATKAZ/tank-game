using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Shared.Stats
{
    public interface IStatTypeParametersConfig
    {
        StatTypeParameters GetParameters(StatType targetType);
    }

    [CreateAssetMenu(
        fileName = "StatTypeParameters",
        menuName = "Scriptable Objects/Stat Type Parameters")]
    public sealed class StatTypeParametersConfig : ScriptableObject, IStatTypeParametersConfig
    {
        [SerializeField] private Dictionary<StatType, StatTypeParameters> _parameters = new();

        public IDictionary<StatType, StatTypeParameters> Parameters => _parameters;

        public StatTypeParameters GetParameters(StatType targetType)
        {
            if (_parameters.TryGetValue(targetType, out StatTypeParameters parameters))
            {
                return parameters;
            }

            return new StatTypeParameters
            {
                MinValue = float.MinValue,
                MaxValue = float.MaxValue,
                DiminishingBase = 0
            };
        }
    }
}
