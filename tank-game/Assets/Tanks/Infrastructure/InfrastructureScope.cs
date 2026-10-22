using NaughtyAttributes;
using System;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Lobbies;
using Tanks.Infrastructure.Processes;
using Tanks.Infrastructure.Repositories;
using Tanks.Infrastructure.Settings;
using Unity.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Infrastructure
{
    public sealed class InfrastructureScope : LifetimeScope
    {
        [Required]
        [SerializeField]
        private UnitySceneLoader _sceneLoader;

        [Required]
        [SerializeField]
        private SessionSettingsService _sessionSettings;

        [Header("Environments")]
        [SerializeField] private EnvironmentSettings _production;
#if DEBUG
        [SerializeField] private EnvironmentSettings _development;
#endif

        private void AddUseCases(IContainerBuilder builder)
        {
            builder.Register<IRunServerUseCase, RunServerUseCase>(Lifetime.Singleton);
            builder.Register<IPrepareServerUseCase, PrepareServerUseCase>(Lifetime.Singleton);
            builder.Register<IConnectToServerUseCase, ConnectToServerUseCase>(Lifetime.Singleton);
            builder.Register<IRenameLobbyUseCase, RenameLobbyUseCase>(Lifetime.Singleton);
        }

        private void AddServices(IContainerBuilder builder)
        {
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;

            builder.Register<IFileSystem, DriveFileSystem>(Lifetime.Singleton);
            builder.Register<IResourceLoader, AddressablesLoader>(Lifetime.Singleton);

            builder.Register<ILobbyService, UGSLobbyService>(Lifetime.Singleton);
            builder.Register<IAuthService, UGSAuthService>(Lifetime.Singleton);

            if (role is MultiplayerRoleFlags.Client)
            {
                builder.Register<
                    IClientCommunicationService,
                    ClientPipeCommunicationService>(Lifetime.Singleton);
            }
            else
            {
                builder.Register<
                    IServerCommunicationService,
                    ServerPipeCommunicationService>(Lifetime.Singleton);
            }

            builder.Register<IProcessService, ProcessService>(Lifetime.Singleton);
            builder.Register<ISettingsLoader, JsonSettingsLoader>(Lifetime.Singleton);
            builder.Register<ISettingsService, JsonSettingsService>(Lifetime.Singleton);

            builder.RegisterComponent(_sessionSettings).As<ISessionSettingsService>();
            builder.RegisterComponent(_sceneLoader).As<ISceneLoader>();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            EnvironmentSettings env;
#if DEBUG
            env = _development;
#else
            env = _production;
#endif

            CcdManager.EnvironmentName = env.EnvironmentName;
            CcdManager.BucketId = env.GetBucketId();
            CcdManager.Badge = "latest";

            AddServices(builder);
            AddUseCases(builder);

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<ISettingsService>().Initialize();
            });
        }
    }
}
