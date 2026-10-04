using System;
using UnityEngine;

namespace Tanks.Shared.Stats
{
    [Serializable]
    public struct StatTypeParameters
    {
        public float MinValue;
        public float MaxValue;

        [Range(0, 100)]
        public int DiminishingBase;
    }
}
