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
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<DamageTaken>,
        INetworkMessageHandler<HPHealed>
    {
        public void Handle(DamageTaken message)
        {
            PositiveFloatOrZero oldValue = Behaviour.Value;
            PositiveFloat maxHealth = Behaviour.MaxHealth;

            Behaviour.SetCurrentHealth(math.clamp(oldValue - message.Amount, 0, maxHealth));
            if (Behaviour.Value > 0) return;

            BaseRpcTarget target = Behaviour.RpcTarget
                .Single(Behaviour.OwnerClientId, RpcTargetUse.Temp);

            Behaviour.SendDiedEveryoneRpc();
            Behaviour.SendDiedLocalClientRpc(target);
            Behaviour.Despawn();
        }

        public void Handle(HPHealed message)
        {
            PositiveFloatOrZero oldValue = Behaviour.Value;
            PositiveFloat maxHealth = Behaviour.MaxHealth;

            Behaviour.SetCurrentHealth(math.clamp(oldValue + message.Amount, 0, maxHealth));
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<DamageTaken>(this);
            Behaviour.RegisterMessageHandler<HPHealed>(this);
        }
    }
}
