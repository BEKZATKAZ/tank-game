using NaughtyAttributes;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Lobbies;
using Tanks.Infrastructure.Repositories;
using Tanks.Infrastructure.Settings;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Infrastructure
{
    public sealed class InfrastructureScope : LifetimeScope
    {
        [Required]
        [SerializeField]
        private SessionSettingsService _sessionSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IFileSystem, DriveFileSystem>(Lifetime.Singleton);
            builder.Register<IResourceLoader, AddressablesLoader>(Lifetime.Singleton);

            builder.Register<IPlayFabCaller, PlayFabCaller>(Lifetime.Singleton);
            builder.Register<ILobbyService, PlayFabLobbyService>(Lifetime.Singleton);

            builder.Register<IAuthService, PlayFabAuthService>(Lifetime.Singleton);
            builder.Register<ISessionConnectionService, SessionConnectionService>(Lifetime.Singleton);

            builder.Register<ISettingsLoader, JsonSettingsLoader>(Lifetime.Singleton);
            builder.Register<ISettingsService, JsonSettingsService>(Lifetime.Singleton);
            builder.RegisterComponent(_sessionSettings).As<ISessionSettingsService>();

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<ISettingsService>().Initialize();
            });
        }
    }
}
