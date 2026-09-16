using Unity.Collections;
using Unity.Netcode;

namespace Tanks.Shared.Teams
{
    public struct AddPlayerPayload : INetworkSerializeByMemcpy
    {
        public TeamId BelongsTo;
        public ulong OwnerPlayerId;
        public FixedString128Bytes DisplayName;
    }
}
