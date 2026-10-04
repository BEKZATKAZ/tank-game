using Tanks.Client.Visuals.Hitstop;
using Tanks.Shared.Weapons.Projectiles.Modules;
using VContainer;

namespace Tanks.Client.Weapons.Projectiles.Modules
{
    public sealed class HitstopModule : ProjectileModuleBehaviourBase
    {
        public float DurationSeconds = 0.3f;

        private IHitstopService _service;

        [Inject]
        public void Construct(IHitstopService service)
        {
            _service = service;
        }

        protected override void OnProjectileDone()
        {
            if (DurationSeconds <= 0) return;
            _service.RunAsync(Transform.WorldPosition, DurationSeconds);
        }
    }
}
