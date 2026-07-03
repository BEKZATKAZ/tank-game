using FMODUnity;
using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFireSound :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        private static readonly EventReference _reference = RuntimeManager
            .PathToEventReference("event:/SFX/DamageTaken");

        public void Handle(WeaponUsed message)
        {
            RuntimeManager.PlayOneShot(_reference);
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}