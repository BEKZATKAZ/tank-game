using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Weapons
{
    public sealed class WeaponMuzzleFlash :
        NetworkComponent<IWeaponHolderBehaviour>, INetworkMessageHandler<WeaponUsed>
    {
        public WeaponMuzzleFlash(IVisualService service)
        {
            _service = service;
        }

        private readonly IVisualService _service;

        public void Handle(WeaponUsed message)
        {
            var visual = _service.Play<ParticleSystemWithLight>(
                VisualType.MuzzleFlash_1,
                position: Behaviour.MuzzlePoint.WorldPosition,
                rotation: Behaviour.MuzzlePoint.WorldRotation);

            visual.StickTo(Behaviour.MuzzlePoint);
            visual.Play();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
