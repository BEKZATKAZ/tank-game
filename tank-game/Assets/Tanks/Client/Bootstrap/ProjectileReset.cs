using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Client.Bootstrap
{
    public sealed class ProjectileReset :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        public ProjectileReset(IProjectilePoolService pool)
        {
            _pool = pool;
        }

        private readonly IProjectilePoolService _pool;

        public void Handle(TilemapGenerated message)
        {
            _pool.ReturnAll();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
