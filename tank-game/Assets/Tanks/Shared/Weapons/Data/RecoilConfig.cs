using System;
using UnityEngine;

namespace Tanks.Shared.Weapons.Data
{
    public interface IRecoilConfig
    {
        float Amount { get; }
        float DurationSeconds { get; }
    }

    [Serializable]
    public class RecoilConfig : IRecoilConfig
    {
        [SerializeField]
        [Range(0, 50)]
        private float _amount = 10;

        [SerializeField]
        [Range(0.1f, 3)]
        private float _durationSeconds = 0.9f;

        public float Amount => _amount;
        public float DurationSeconds => _durationSeconds;
    }
}