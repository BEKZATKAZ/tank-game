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
        public float ServerDuration;
        public int ServerTick;
    }

    public interface IDangerZoneBehaviour : INetworkBehaviourExtended
    {
        float StartDelay { get; }
        float ShrinkDuration { get; }
        float Damage { get; }
        Rect CurrentState { get; }

        void SetCurrentState(Rect rect);
        void StartShrinkingClientRpc(DangerZoneStarted message);
    }

    public sealed class DangerZoneBehaviour :
        NetworkBehaviourExtended, IDangerZoneBehaviour, IMessageSender
    {
        [Range(0, 60)]
        [SerializeField]
        private float _startDelay = 15;

        [Range(120, 20)]
        [SerializeField]
        private float _shrinkDuration = 40;

        [Range(2, 25)]
        [SerializeField]
        private float _damage = 10;

        [Inject] private readonly IMessageBus _bus;

        public float StartDelay => _startDelay;
        public float ShrinkDuration => _shrinkDuration;
        public float Damage => _damage;
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
