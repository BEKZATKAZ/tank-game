using Tanks.Shared;
using Tanks.Shared.Entities;

namespace Tanks.Server.Lag
{
    public sealed class LagCompensationEntity : NetworkComponent<IEntityBehaviour>
    {
        public LagCompensationEntity(ILagCompensationService service)
        {
            _service = service;
        }

        private readonly ILagCompensationService _service;

        protected override void OnLateTick(int tick, float tickDelta)
        {
            EntitySnapshot snapshot = new EntitySnapshot(
                Transform.WorldPosition,
                Transform.WorldRotation,
                Transform.WorldScale * Behaviour.Size);

            _service.CaptureEntity(Behaviour.Id, tick, snapshot);
        }
    }
}
