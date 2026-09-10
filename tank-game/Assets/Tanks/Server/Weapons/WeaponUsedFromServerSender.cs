using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public sealed class WeaponUsedFromServerSender :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        public void Handle(WeaponUsed message)
        {
            Behaviour.UseWeaponClientRpc(new WeaponUsedFromServer { Id = message.ShotId });
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
