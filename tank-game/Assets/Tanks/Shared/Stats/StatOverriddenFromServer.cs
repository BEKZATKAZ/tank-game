using Unity.Netcode;

namespace Tanks.Shared.Stats
{
    public struct StatOverriddenFromServer : INetworkSerializeByMemcpy
    {
        public StatId Id;
        public StatType Type;
        public StatValueType ValueType;
        public float Value;
        public float Duration;
    }
}
