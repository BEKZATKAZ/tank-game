using Tanks.Core.Helpers;
using Tanks.Shared.Messaging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using VContainer;

namespace Tanks.Shared.Health
{
    public struct DamageTaken
    {
        public PositiveFloat Amount;
    }

    public struct HPHealed
    {
        public PositiveFloat Amount;
    }

    public struct Died
    {
        public IDamageable Damageable;
    }

    public struct DiedLocal
    {
        
    }

    [RequireComponent(typeof(NetworkObject))]
    public class HealthBehaviour :
        NetworkBehaviourExtended, IDamageable, IHealable, IMessageSender
    {
        [Range(10, 200)]
        [SerializeField]
        private float _maxHealth = 100;

        [Inject]
        private readonly IMessageBus _bus;

        public NetworkVariable<float> Health { get; } = new(100);
        public PositiveFloatOrZero MaxHealth => _maxHealth;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void SendDiedEveryoneRpc()
        {
            EmitMessage(new Died { Damageable = this });
            _bus.Publish(new Died { Damageable = this }, this);
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        public void SendDiedLocalClientRpc(RpcParams p = default)
        {
            EmitMessage(new DiedLocal());
            _bus.Publish(new DiedLocal(), this);
        }

        public void Damage(PositiveFloat amount)
        {
            EmitMessage(new DamageTaken { Amount = amount });
        }

        public void Heal(PositiveFloat amount)
        {
            EmitMessage(new HPHealed { Amount = amount });
        }
    }
}
