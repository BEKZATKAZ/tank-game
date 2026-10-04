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
        public IEntityBehaviour From;
        public HealthState OldState;
        public HealthState AuthoritativeState;
        public HealthEventId EventId;
    }

    public interface IHealthBehaviour : INetworkBehaviourExtended
    {
        IEntityBehaviour Entity { get; }
        IReadOnlyBindableProperty<HealthState> ServerState { get; }

        IEnumerable<SpriteRenderer> GetAllSpriteRenderers();

        void UpdateStateEveryoneRpc(
            HealthState state,
            EntityId from = default,
            HealthEventId eventId = default);
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public class HealthBehaviour : NetworkBehaviourExtended, IHealthBehaviour, IMessageSender
    {
        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IEntityService _entityService;

        public IEntityBehaviour Entity { get; private set; }
        public IReadOnlyBindableProperty<HealthState> ServerState => _serverState;

        private readonly BindableProperty<HealthState> _serverState = new();

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void UpdateStateEveryoneRpc(
            HealthState newState,
            EntityId from = default,
            HealthEventId eventId = default)
        {
            HealthState oldState = _serverState.Value;

            // Avoid constant reduntant updates
            if (oldState.Equals(newState)) return;

            _serverState.Value = newState;
            IEntityBehaviour fromEntity = _entityService.GetById(from);

            HealthChangedFromServer message = new HealthChangedFromServer
            {
                EventId = eventId,
                Target = Entity,
                From = fromEntity,
                OldState = oldState,
                AuthoritativeState = newState
            };

            EmitMessage(message);
            _bus.Publish(message, this);

            if (newState.Current > 0) return;

            Died died = new Died
            {
                Target = Entity,
                From = fromEntity,
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
