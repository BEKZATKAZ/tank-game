using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tanks.Shared.Weapons;
using Tanks.Shared.Items;
using Tanks.Shared.Entities;

namespace Tanks.Shared.Bootstrap
{
    public sealed class GameplayScope : GameplayScopeBase
    {
        [SerializeField] private SharedProjectilePoolService _projectilePool;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterComponent(_projectilePool).As<IProjectilePoolService>();
            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Singleton);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Singleton);

            builder.Register<IMessageBus, MessageBus>(Lifetime.Singleton);
            builder.Register<IWeaponFactory, WeaponFactory>(Lifetime.Singleton);
            builder.Register<IItemDatabase, ItemDatabase>(Lifetime.Singleton);
            builder.Register<IEntityService, EntityService>(Lifetime.Singleton);
        }
    }
}
