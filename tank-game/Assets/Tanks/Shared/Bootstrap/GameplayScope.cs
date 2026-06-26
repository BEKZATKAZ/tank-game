using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Bootstrap
{
    public sealed class GameplayScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Scoped);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Scoped);
        }
    }
}