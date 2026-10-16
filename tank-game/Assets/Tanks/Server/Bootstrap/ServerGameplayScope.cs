using NaughtyAttributes;
using Tanks.Server.Entities;
using Tanks.Server.Generation;
using Tanks.Server.Generation.Crates;
using Tanks.Server.Lag;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Pickups;
using Unity.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Server.Bootstrap
{
    public sealed class ServerGameplayScope : GameplayScope
    {
        [Required]
        [SerializeField]
        private UnityServerLifetimeService _lifetime;

        [Required]
        [SerializeField]
        private PickupSpawnBehaviour _pickupSpawner;

        [Required]
        [SerializeField]
        private CrateSpawner _crateSpawner;

        protected override void Configure(IContainerBuilder builder)
        {
#if UNITY_EDITOR
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            if (role is MultiplayerRoleFlags.Client) return;
#endif

            base.Configure(builder);

            builder.RegisterComponent(_lifetime).As<IServerLifetimeService>();
            builder.RegisterComponent(_pickupSpawner).As<IPickupSpawnBehaviour>();
            builder.RegisterComponent(_crateSpawner).As<ICrateSpawner>();

            builder.Register<
                IClientConnectionValidator, 
                ClientConnectionValidator>(Lifetime.Singleton);

            builder.Register<
                IConnectedClientService,
                ConnectedClientService>(Lifetime.Singleton);

            builder.Register<IHitSimulator, HitSimulator>(Lifetime.Singleton);
            builder.Register<ILagCompensationService, LagCompensationService>(Lifetime.Singleton);
            builder.Register<IDamageService, AuthoritativeDamageService>(Lifetime.Singleton);
            builder.Register<ICrateGenerator, CrateGenerator>(Lifetime.Singleton);
        }
    }
}
