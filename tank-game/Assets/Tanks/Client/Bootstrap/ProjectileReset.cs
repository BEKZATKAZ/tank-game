using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Shared.Weapons.Projectiles;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class ProjectileReset :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<TilemapGenerated>
    {
        [Inject] private readonly IProjectilePoolService _pool;

        public void Handle(TilemapGenerated message)
        {
            _pool.ReturnAll();
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
