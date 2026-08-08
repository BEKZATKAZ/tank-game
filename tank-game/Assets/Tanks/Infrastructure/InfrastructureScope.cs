using Tanks.Core;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Repositories;
using Tanks.Infrastructure.Settings;
using VContainer;

namespace Tanks.Infrastructure
{
    public static class InfrastructureScope
    {
        public static void RegisterInfrastructure(this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<IFileSystem, DriveFileSystem>(lifetime);
            builder.Register<IResourceLoader, AddressablesLoader>(lifetime);

            builder.Register<IPlayFabCaller, PlayFabCaller>(lifetime);
            builder.Register<IAuthService, PlayFabAuthService>(lifetime);

            builder.Register<ISettingsLoader, JsonSettingsLoader>(lifetime);
            builder.Register<ISettingsService, JsonSettingsService>(lifetime);

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<ISettingsService>().Initialize();
            });
        }
    }
}
