using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public sealed class WeaponUsedFromServerSender :
        NetworkComponent<IWeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        private int _lastTick;

        public void Handle(WeaponUsed message)
        {
            Behaviour.UseWeaponClientRpc(new WeaponUsedFromServer
            {
                ServerTick = _lastTick,
                Id = message.ShotId,
                Direction = message.Owner.ShotDirection ?? message.Owner.MuzzlePoint.WorldRotation
            });
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
        }
    }
}
