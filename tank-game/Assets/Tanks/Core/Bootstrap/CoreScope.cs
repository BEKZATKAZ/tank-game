using Tanks.Infrastructure;
using VContainer;
using VContainer.Unity;

namespace Tanks.Core.Bootstrap
{
    public sealed class CoreScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInfrastructure();
        }
    }
}
