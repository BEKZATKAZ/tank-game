using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Weapons
{
    public sealed class ProjectileReturnValidator :
        NetworkComponent<IProjectilePoolService>, INetworkMessageHandler<ProjectileReturnRequested>
    {
        public void Handle(ProjectileReturnRequested message)
        {
            if (Behaviour.TryGetProjectile(message.Id, out IProjectileBehaviour instance) == false)
            {
                return;
            }

            Behaviour.RejectReturnClientRpc(new ProjectileReturnRequestRejected
            {
                Id = message.Id,
                Position = instance.Transform.WorldPosition,
                Rotation = instance.Transform.WorldRotation
            });
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
