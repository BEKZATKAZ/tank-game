using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Lobbies;
using Tanks.Infrastructure.Repositories;
using Tanks.Infrastructure.Settings;
using VContainer;

namespace Tanks.Infrastructure
{
    public static class InfrastructureScope
    {
        public static void RegisterInfrastructure(this IContainerBuilder builder)
        {
            builder.Register<IFileSystem, DriveFileSystem>(Lifetime.Singleton);
            builder.Register<IResourceLoader, AddressablesLoader>(Lifetime.Singleton);

            builder.Register<IPlayFabCaller, PlayFabCaller>(Lifetime.Singleton);
            builder.Register<ILobbyService, PlayFabLobbyService>(Lifetime.Singleton);
            builder.Register<IAuthService, PlayFabAuthService>(Lifetime.Singleton);

            builder.Register<ISettingsLoader, JsonSettingsLoader>(Lifetime.Singleton);
            builder.Register<ISettingsService, JsonSettingsService>(Lifetime.Singleton);

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<ISettingsService>().Initialize();
            });
        }
    }
}
