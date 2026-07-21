using System;
using Unity.Netcode;

namespace Tanks.Shared.Items.Pickups
{
    public readonly struct PickupId : IEquatable<PickupId>, INetworkSerializeByMemcpy
    {
        public PickupId(uint value) => Value = value;

        public readonly uint Value;

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is PickupId other && Equals(other);

        public bool Equals(PickupId other) => Value == other.Value;
    }
}