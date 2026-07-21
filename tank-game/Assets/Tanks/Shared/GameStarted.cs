using Unity.Netcode;

namespace Tanks.Shared
{
    public struct GameStarted : INetworkSerializeByMemcpy
    {
        public float StartDelaySeconds;
    }
}
