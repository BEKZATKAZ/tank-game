using Tanks.Core.Repositories;
using Tanks.Infrastructure.Repositories;
using VContainer;

namespace Tanks.Infrastructure
{
    public static class InfrastructureScope
    {
        public static RegistrationBuilder RegisterInfrastructure(this IContainerBuilder builder)
        {
            return builder.Register<IResourceLoader, AddressablesLoader>(Lifetime.Scoped);
        }
    }
}
