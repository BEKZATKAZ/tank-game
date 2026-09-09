using Unity.Netcode;

namespace Tanks.Shared
{
    public struct GameStartedFromServer : INetworkSerializeByMemcpy
    {
        public int ServerTick;
        public float ServerTickDelta;
        public float StartDelaySeconds;
    }
}
