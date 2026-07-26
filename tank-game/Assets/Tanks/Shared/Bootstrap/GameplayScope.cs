using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using Tanks.Shared.Messaging;
using Tanks.Infrastructure;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tanks.Shared.Weapons;
using Tanks.Shared.Items;

namespace Tanks.Shared.Bootstrap
{
    public sealed class GameplayScope : GameplayScopeBase
    {
        [SerializeField] private SharedProjectilePoolService _projectilePool;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterInfrastructure(Lifetime.Scoped);
            builder.RegisterComponent(_projectilePool).As<IProjectilePoolService>();

            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Scoped);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Scoped);

            builder.Register<IMessageBus, MessageBus>(Lifetime.Scoped);
            builder.Register<IWeaponFactory, WeaponFactory>(Lifetime.Scoped);
            builder.Register<IItemDatabase, ItemDatabase>(Lifetime.Scoped);
        }
    }
}
