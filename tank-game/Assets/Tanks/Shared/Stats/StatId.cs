using System;
using Unity.Netcode;

namespace Tanks.Shared.Stats
{
    public readonly struct StatId :
        IEquatable<StatId>, INetworkSerializeByMemcpy
    {
        public StatId(uint value)
        {
            Value = value;
        }

        public readonly uint Value;

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is StatId other && Equals(other);

        public bool Equals(StatId other) => other.Value == Value;
    }
}
