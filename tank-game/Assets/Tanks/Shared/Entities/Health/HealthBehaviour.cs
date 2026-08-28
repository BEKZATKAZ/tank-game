using System.Collections.Generic;
using Tanks.Core.Helpers;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Entities.Health
{
    public struct DamageTaken
    {
        public PositiveFloat Amount;
        public IEntity From;
    }

    public struct HPHealed
    {
        public PositiveFloat Amount;
    }

    public struct Died
    {
        public IDamageable Damageable;
        public IEntity From;
        public float2 WorldPosition;
    }

    public struct DiedLocal
    {
        
    }

    public interface IHealthBehaviour : INetworkBehaviourExtended, IHealth, IDamageable, IHealable
    {
        IEntity Entity { get; }
        RpcTarget RpcTarget { get; }

        IEnumerable<SpriteRenderer> GetAllSpriteRenderers();
        void Despawn();
        void SendDiedEveryoneRpc(EntityId from);
        void SendDiedLocalClientRpc(RpcParams p = default);
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public class HealthBehaviour : NetworkBehaviourExtended, IHealthBehaviour, IMessageSender
    {
        [Range(10, 200)]
        [SerializeField]
        private float _maxHealth = 100;

        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IEntityService _entityService;
        
        public IEntity Entity { get; private set; }
        public PositiveFloat MaxHealth => _maxHealth;
        public PositiveFloatOrZero Value => _health.Value;

        private readonly NetworkVariable<float> _health = new(100);

        public void Despawn()
        {
            NetworkObject.Despawn();
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SendDiedEveryoneRpc(EntityId from)
        {
            Died died = new Died
            {
                Damageable = this,
                From = _entityService.GetById(from),
                WorldPosition = Transform.WorldPosition
            };

            EmitMessage(died);
            _bus.Publish(died, this);
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        public void SendDiedLocalClientRpc(RpcParams p = default)
        {
            EmitMessage(new DiedLocal());
            _bus.Publish(new DiedLocal(), this);
        }

        public void SetCurrentHealth(PositiveFloatOrZero value)
        {
            _health.Value = math.clamp(value, 0, MaxHealth);
        }

        public void SetMaxHealth(PositiveFloat value)
        {
            _maxHealth = value;
            float remainder = _health.Value - _maxHealth;

            // Damage to compensate max health
            if (remainder > 0) Damage(remainder);
        }

        public void Damage(PositiveFloat amount, IEntity from = null)
        {
            EmitMessage(new DamageTaken { Amount = amount, From = from });
        }

        public void Heal(PositiveFloat amount)
        {
            EmitMessage(new HPHealed { Amount = amount });
        }

        public IEnumerable<SpriteRenderer> GetAllSpriteRenderers()
        {
            return GetComponentsInChildren<SpriteRenderer>();
        }

        protected override void Awake()
        {
            base.Awake();
            Entity = GetComponent<EntityBehaviour>();
        }
    }
}
