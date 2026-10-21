using Tanks.UI.Playground;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Gameplay
{
    public sealed class PlaygroundUIScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PlaygroundViewModel>(Lifetime.Transient);
        }
    }
}
