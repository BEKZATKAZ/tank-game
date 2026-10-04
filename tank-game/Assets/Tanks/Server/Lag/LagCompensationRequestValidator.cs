using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Lag;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;

namespace Tanks.Server.Lag
{
    public sealed class LagCompensationRequestValidator :
        NetworkComponent<ILagCompensationGateway>,
        INetworkMessageHandler<HitRequest>
    {
        public LagCompensationRequestValidator(
            ILagCompensationService service,
            IProjectilePoolService projectilePool,
            IEntityService entityService)
        {
            _service = service;
            _projectilePool = projectilePool;
            _entityService = entityService;
        }

        private readonly ILagCompensationService _service;
        private readonly IProjectilePoolService _projectilePool;
        private readonly IEntityService _entityService;

        public void Handle(HitRequest message)
        {
            EntityId targetId = message.TargetId;
            ProjectileId projectileId = message.ProjectileId;

            if (_service.IsHitAtTick(
                targetId, projectileId, message.ClientTick, out int usedTick) == false) return;

            UnityEngine.Debug.LogFormat("Hit was valid at {0}, compensating lag...", usedTick);

            IEntityBehaviour target = _entityService.GetById(message.TargetId);
            if (target is not { IsValid: true }) return;

            ProjectileData data = _service.GetProjectileData(message.ProjectileId);

            var modules = data.Behaviour.GetBehaviour<IProjectileModuleList>();
            modules.Call(module => module.HitEntity(target, data.Owner.Entity));

            _projectilePool.ReturnProjectileClientRpc(projectileId);
            _projectilePool.Return(projectileId);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
