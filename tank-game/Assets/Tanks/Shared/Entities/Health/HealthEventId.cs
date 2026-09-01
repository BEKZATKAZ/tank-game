using System;
using Unity.Netcode;

namespace Tanks.Shared.Entities.Health
{
    public readonly struct HealthEventId : IEquatable<HealthEventId>, INetworkSerializeByMemcpy
    {
        private HealthEventId(int value) => Value = value;

        public readonly int Value;

        public bool Equals(HealthEventId other) => other.Value == Value;

        public static HealthEventId Create<TCaller, TValue>(TCaller caller, TValue value)
        {
            string name = caller.GetType().Name;

            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < name.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ name[i];
                    if (i + 1 < name.Length)
                    {
                        hash2 = ((hash2 << 5) + hash2) ^ name[i + 1];
                    }
                }

                int typeCode = hash1 + (hash2 * 1566083941);
                int valueHash = value.GetHashCode();

                return new HealthEventId(typeCode + valueHash);
            }
        }
    }
}
