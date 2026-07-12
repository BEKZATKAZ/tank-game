using FMODUnity;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using UnityEngine;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponFireSound :
        NetworkComponent<WeaponHolderBehaviour>,
        INetworkMessageHandler<WeaponUsed>
    {
        private static readonly EventReference _reference = RuntimeManager
            .PathToEventReference("event:/SFX/Shoot");

        public void Handle(WeaponUsed message)
        {
            RuntimeManager.PlayOneShot(_reference, (Vector2)Behaviour.Transform.WorldPosition);
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
