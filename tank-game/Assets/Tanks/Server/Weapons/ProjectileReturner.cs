using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Weapons
{
    public sealed class ProjectileReturner :
        NetworkComponent<IProjectilePoolService>, INetworkMessageHandler<ProjectileReturned>
    {
        public void Handle(ProjectileReturned message)
        {
            Behaviour.ReturnProjectileClientRpc(message.Id);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
