using Tanks.Shared.Adapters;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Shared.Lag
{
    public struct Test : INetworkSerializeByMemcpy
    {
        public float2 TargetPosition;
        public Angle TargetRotation;
        public float2 TargetSize;

        public float2 ProjectilePosition;
        public float ProjectileRadius;
    }

    public interface ILagCompensationGateway : INetworkBehaviourExtended
    {
        void RequestHitServerRpc(HitRequest request);
    }
}
