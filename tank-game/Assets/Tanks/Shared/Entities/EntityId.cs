using System;
using Unity.Netcode;

namespace Tanks.Shared.Entities
{
    public readonly struct EntityId : IEquatable<EntityId>, INetworkSerializeByMemcpy
    {
        public EntityId(int value) => Value = value;

        public readonly int Value;

        public static bool operator ==(in EntityId lhs, in EntityId rhs) => lhs.Value == rhs.Value;
        public static bool operator !=(in EntityId lhs, in EntityId rhs) => lhs.Value != rhs.Value;

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public bool Equals(EntityId other) => other.Value == Value;
    }
}