using Unity.Netcode;

namespace Tanks.Shared.Teams
{
    public struct UpdatePlayerPointsPayload : INetworkSerializeByMemcpy
    {
        public ulong TargetPlayer;
        public int Eliminations;
        public int Assistance;
        public int Deaths;
    }
}
