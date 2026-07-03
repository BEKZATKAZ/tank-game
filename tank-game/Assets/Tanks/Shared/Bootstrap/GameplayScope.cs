using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Bootstrap
{
    public sealed class GameplayScope : GameplayScopeBase
    {
        [SerializeField] private UnityProjectilePoolService _projectilePool;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Scoped);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Scoped);
            builder.Register<IMessageBus, MessageBus>(Lifetime.Scoped);

            builder.RegisterComponent(_projectilePool).As<IProjectilePoolService>();
        }
    }
}