using Tanks.Core.Helpers;
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
            PositiveFloatOrZero oldValue = Behaviour.Health.Value;
            PositiveFloatOrZero maxHealth = Behaviour.MaxHealth;

            Behaviour.Health.Value = math.clamp(oldValue - message.Amount, 0, maxHealth);
            if (Behaviour.Health.Value > 0) return;

            BaseRpcTarget target = Behaviour.RpcTarget
                .Single(Behaviour.OwnerClientId, RpcTargetUse.Temp);

            Behaviour.SendDiedEveryoneRpc();
            Behaviour.SendDiedLocalClientRpc(target);
            Behaviour.GetComponent<NetworkObject>().Despawn();
        }

        public void Handle(HPHealed message)
        {
            PositiveFloatOrZero oldValue = Behaviour.Health.Value;
            PositiveFloatOrZero maxHealth = Behaviour.MaxHealth;

            Behaviour.Health.Value = math.clamp(oldValue + message.Amount, 0, maxHealth);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<DamageTaken>(this);
            Behaviour.RegisterMessageHandler<HPHealed>(this);
        }
    }
}
