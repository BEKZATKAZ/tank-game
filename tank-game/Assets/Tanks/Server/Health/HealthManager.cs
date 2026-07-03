using Tanks.Shared;
using Tanks.Shared.Health;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Health
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class HealthManager :
        NetworkComponent<HealthBehaviour>,
        INetworkMessageHandler<DamageTaken>,
        INetworkMessageHandler<HPHealed>
    {
        public void Handle(DamageTaken message)
        {
            float oldValue = Behaviour.Health.Value;
            float maxHealth = Behaviour.MaxHealth;

            Behaviour.Health.Value = math.clamp(oldValue - message.Amount, 0, maxHealth);
            if (Behaviour.Health.Value > 0) return;

            GetComponent<NetworkObject>().Despawn();
        }

        public void Handle(HPHealed message)
        {
            float oldValue = Behaviour.Health.Value;
            float maxHealth = Behaviour.MaxHealth;

            Behaviour.Health.Value = math.clamp(oldValue + message.Amount, 0, maxHealth);
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler<DamageTaken>(this);
            Behaviour.RegisterMessageHandler<HPHealed>(this);
        }
    }
}
