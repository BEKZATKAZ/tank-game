using System;
using Tanks.Core.Helpers;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Shared.Entities.Health
{
    public readonly struct HealthState : IEquatable<HealthState>, INetworkSerializeByMemcpy
    {
        public HealthState(float current, PositiveFloat max)
        {
            Current = math.min(current, max);
            Max = max;
        }

        public readonly float NormalizedCurrent => Current / Max;
        public readonly float Current;
        public readonly float Max;

        public override string ToString() => $"{{ Current: {Current}, Max: {Max} }}";
        public override int GetHashCode() => HashCode.Combine(Current, Max);
        public override bool Equals(object obj) => obj is HealthState other && Equals(other);

        public bool Equals(HealthState other) => Current == other.Current && Max == other.Max;
    }
}
