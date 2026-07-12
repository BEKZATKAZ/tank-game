using NaughtyAttributes;
using System;
using UnityEngine;

namespace Tanks.Shared.Health.Data
{
    public enum DamageType
    {
        NearFar,
        Even
    }

    [CreateAssetMenu(fileName = "Damage", menuName = "Scriptable Objects/Health/Damage")]
    public class DamageConfig : ScriptableObject
    {
        [SerializeField]
        private DamageType _type;

        [ShowIf("_type", DamageType.Even)]
        [Range(1, 100)]
        [SerializeField]
        private float _amount = 10;

        [ShowIf("_type", DamageType.NearFar)]
        [Range(1, 100)]
        [SerializeField]
        private float _nearAmount = 20;

        [ShowIf("_type", DamageType.NearFar)]
        [Range(1, 100)]
        [SerializeField]
        private float _farAmount = 5;

        [ShowIf("_type", DamageType.NearFar)]
        [Range(0.1f, 10)]
        [SerializeField]
        private float _minDistance = 1;

        [ShowIf("_type", DamageType.NearFar)]
        [Range(1, 20)]
        [SerializeField]
        private float _maxDistance = 10;

        public IDamage Create()
        {
            return _type switch
            {
                DamageType.NearFar => new NearFarDamage(
                    _nearAmount, _farAmount, _minDistance, _maxDistance),

                DamageType.Even => new NearFarDamage(_amount, _amount, 1, 1),

                _ => throw new NotImplementedException()
            };
        }
    }
}
