using System;
using Unity.Netcode;

namespace Tanks.Shared.Teams
{
    public readonly struct TeamId : IEquatable<TeamId>, INetworkSerializeByMemcpy
    {
        public TeamId(ushort value) => Value = value;

        public readonly ushort Value;

        public static bool operator ==(in TeamId lhs, in TeamId rhs) => lhs.Equals(rhs);
        public static bool operator !=(in TeamId lhs, in TeamId rhs) => !lhs.Equals(rhs);

        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is TeamId other && Equals(other);

        public bool Equals(TeamId other) => Value == other.Value;
    }
}
