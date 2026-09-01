using System.Collections.Generic;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Entities.Health
{
    public struct Died
    {
        public IEntityBehaviour Target;
        public IEntityBehaviour From;
        public float2 WorldPosition;
    }

    public struct HealthChangedFromServer
    {
        public IEntityBehaviour Target;
        public HealthState OldState;
        public HealthState AuthoritativeState;
    }

    public interface IHealthBehaviour : INetworkBehaviourExtended
    {
        IEntityBehaviour Entity { get; }
        IReadOnlyBindableProperty<HealthState> ServerState { get; }

        IEnumerable<SpriteRenderer> GetAllSpriteRenderers();
        void UpdateStateEveryoneRpc(int tick, HealthState state, EntityId from = default);
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public class HealthBehaviour : NetworkBehaviourExtended, IHealthBehaviour, IMessageSender
    {
        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IEntityService _entityService;

        public IEntityBehaviour Entity { get; private set; }
        public IReadOnlyBindableProperty<HealthState> ServerState => _serverState;

        private readonly BindableProperty<HealthState> _serverState = new();
        private int _lastProcessedTick;

        [Rpc(SendTo.Everyone,
            Delivery = RpcDelivery.Unreliable,
            InvokePermission = RpcInvokePermission.Server)]
        public void UpdateStateEveryoneRpc(int tick, HealthState newState, EntityId from = default)
        {
            HealthState oldState = _serverState.Value;

            // Avoid constant reduntant updates
            if (oldState.Equals(newState)) return;

            // Discard older packets
            if (_lastProcessedTick >= tick) return;

            _serverState.Value = newState;
            _lastProcessedTick = tick;

            EmitMessage(new HealthChangedFromServer
            {
                Target = Entity,
                OldState = oldState,
                AuthoritativeState = newState
            });

            if (newState.Current > 0) return;

            Died died = new Died
            {
                Target = Entity,
                From = _entityService.GetById(from),
                WorldPosition = Transform.WorldPosition
            };

            EmitMessage(died);
            _bus.Publish(died, this);

            if (IsServer) NetworkObject.Despawn();
        }

        public IEnumerable<SpriteRenderer> GetAllSpriteRenderers()
        {
            return GetComponentsInChildren<SpriteRenderer>();
        }

        protected override void Awake()
        {
            base.Awake();
            Entity = GetComponent<EntityBehaviour>();
            _serverState.Value = new HealthState(100, 100);
        }
    }
}
