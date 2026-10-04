using Tanks.Client.Rendering;
using Tanks.Shared.Weapons.Projectiles.Modules;
using VContainer;

namespace Tanks.Client.Weapons.Projectiles.Modules
{
    public sealed class ShakeModule : ProjectileModuleBehaviourBase
    {
        public byte Radius;
        public byte DurationSeconds;
        public byte Amplitude;
        public byte Frequency;

        private ICameraShakeService _service;

        [Inject]
        public void Construct(ICameraShakeService service)
        {
            _service = service;
        }

        protected override void OnProjectileDone()
        {
            if (Amplitude == 0) return;

            _service.AddShake(new CameraImpulse(
                Transform.WorldPosition, Amplitude, Frequency, Radius, DurationSeconds));
        }
    }
}
