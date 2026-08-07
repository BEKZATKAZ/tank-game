using Tanks.Core;
using Tanks.Core.Repositories;
using Tanks.Infrastructure.Repositories;
using VContainer;

namespace Tanks.Infrastructure
{
    public static class InfrastructureScope
    {
        public static void RegisterInfrastructure(this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<IResourceLoader, AddressablesLoader>(lifetime);
            builder.Register<IPlayFabCaller, PlayFabCaller>(lifetime);
            builder.Register<IAuthService, PlayFabAuthService>(lifetime);
        }
    }
}
