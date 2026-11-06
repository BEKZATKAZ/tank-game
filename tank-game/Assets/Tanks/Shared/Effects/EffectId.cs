using System;
using Unity.Netcode;

namespace Tanks.Shared.Effects
{
    public readonly struct EffectId :
        IEquatable<EffectId>, INetworkSerializeByMemcpy
    {
        public EffectId(uint value)
        {
            Value = value;
        }

        public readonly uint Value;

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is EffectId other && Equals(other);

        public bool Equals(EffectId other) => other.Value == Value;
    }
}
