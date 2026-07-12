using System;
using Unity.Netcode;

namespace Tanks.Shared.Weapons
{
    public readonly struct ShotId : IEquatable<ShotId>, INetworkSerializeByMemcpy
    {
        public ShotId(int value) => Value = value;

        public readonly int Value { get; }

        public static bool operator ==(in ShotId lhs, in ShotId rhs) => lhs.Equals(rhs);
        public static bool operator !=(in ShotId lhs, in ShotId rhs) => !lhs.Equals(rhs);

        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is ShotId other && Equals(other);
        public bool Equals(ShotId other) => Value == other.Value;
    }
}
