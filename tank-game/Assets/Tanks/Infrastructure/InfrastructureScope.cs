using NaughtyAttributes;
using System;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Debriefing;
using Tanks.Core.Leaderboards;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.UseCases;
using Tanks.Core.Players;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Core.Store;
using Tanks.Infrastructure.Debriefing;
using Tanks.Infrastructure.Leaderboards;
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
        [SerializeField] private EnvironmentSettings _development;

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
            builder.Register<GameplaySessionService>(Lifetime.Singleton);
            builder.Register<ICloudCodeCaller, UGSCloudCodeCaller>(Lifetime.Singleton);
            builder.Register<IDebriefingService,
                UGSDebriefingService>(Lifetime.Singleton);

            builder.Register<IPendingDebriefingService, PendingDebriefingService>(
                Lifetime.Singleton);

            builder.Register<IDebriefingClockService,
                DebriefingClockService>(Lifetime.Singleton);

            builder.Register<IPlayerLevelService, PlayerLevelService>(
                Lifetime.Singleton);

            builder.Register<IServerClock, ServerClock>(Lifetime.Singleton);
            builder.Register<UGSPlayerService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<MenuLoadingState>(Lifetime.Singleton);
            builder.Register<ICountryService, IpCountryService>(Lifetime.Singleton);
            builder.Register<ILeaderboardService, UGSLeaderboardService>(Lifetime.Singleton);

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
            builder.Register<IFrameRateController, UnityFrameRateController>(Lifetime.Singleton);
            builder.Register<IFrameRateLimitService, FrameRateLimitService>(Lifetime.Singleton);

            builder.RegisterComponent(_sessionSettings).As<ISessionSettingsService>();
            builder.RegisterComponent(_sceneLoader).As<ISceneLoader>();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            EnvironmentSettings env = EnvironmentSettings.Select(
                _production, _development, Application.isEditor, Debug.isDebugBuild);

            builder.RegisterInstance(env);

            CcdManager.EnvironmentName = env.EnvironmentName;
            CcdManager.BucketId = env.GetBucketId();
            CcdManager.Badge = $"release-{Application.version.Replace('.', '-')}";

            AddServices(builder);
            AddUseCases(builder);

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<ISettingsService>().Initialize();
            });
        }
    }
}
