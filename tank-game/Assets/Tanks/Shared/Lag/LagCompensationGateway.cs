using Unity.Netcode;

namespace Tanks.Shared.Lag
{
    public sealed class LagCompensationGateway : NetworkBehaviourExtended, ILagCompensationGateway
    {
        [Rpc(SendTo.Server)]
        public void RequestHitServerRpc(HitRequest request)
        {
            EmitMessage(request);
        }
    }
}
