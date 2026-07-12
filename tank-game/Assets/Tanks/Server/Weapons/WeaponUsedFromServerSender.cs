using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public sealed class WeaponUsedFromServerSender :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        public void Handle(WeaponUsed message)
        {
            Behaviour.UseWeaponClientRpc(new WeaponUsedFromServer { Id = message.ShotId });
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
