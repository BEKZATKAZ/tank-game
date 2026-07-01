using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using VContainer;

namespace Tanks.Shared.Bootstrap
{
    public sealed class GameplayScope : GameplayScopeBase
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Scoped);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Scoped);
        }
    }
}