using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Lag
{
    public sealed class LagCompensationProjectile : NetworkComponent<IProjectileBehaviour>
    {
        public LagCompensationProjectile(ILagCompensationService service)
        {
            _service = service;
        }

        private readonly ILagCompensationService _service;

        protected override void OnLateTick(int tick, float tickDelta)
        {
            ProjectileSnapshot snapshot = new ProjectileSnapshot(
                Transform.WorldPosition,
                Behaviour.Radius,
                Behaviour.ElapsedSeconds);

            ProjectileData data = new ProjectileData
            {
                Behaviour = Behaviour,
                Owner = Behaviour.Owner
            };

            _service.CaptureProjectile(Behaviour.Id, data, tick, snapshot);
        }
    }
}
