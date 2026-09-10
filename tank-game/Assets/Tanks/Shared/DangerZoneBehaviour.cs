using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared
{
    public struct DangerZoneStarted : INetworkSerializeByMemcpy
    {
        public float2 StartSize;
        public int ServerTick;
    }

    public interface IDangerZoneBehaviour : INetworkBehaviourExtended
    {
        Rect CurrentState { get; }

        void SetCurrentState(Rect rect);
        void StartShrinkingClientRpc(DangerZoneStarted message);
    }

    public sealed class DangerZoneBehaviour :
        NetworkBehaviourExtended, IDangerZoneBehaviour, IMessageSender
    {
        [Inject] private readonly IMessageBus _bus;

        public Rect CurrentState { get; private set; }

        public void SetCurrentState(Rect rect)
        {
            CurrentState = rect;
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        public void StartShrinkingClientRpc(DangerZoneStarted message)
        {
            EmitMessage(message);
            _bus.Publish(message, this);
        }
    }
}
