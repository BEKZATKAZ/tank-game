using Tanks.Shared.Generation;
using Tanks.Shared.Generation.Mazes;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tanks.Shared.Weapons;
using Tanks.Shared.Items;
using Tanks.Shared.Entities;
using NaughtyAttributes;
using Tanks.Shared.Teams;
using Tanks.Shared.Lag;

namespace Tanks.Shared.Bootstrap
{
    public abstract class GameplayScope : LifetimeScope
    {
        [Required]
        [SerializeField]
        private SharedProjectilePoolService _projectilePool;

        [Required]
        [SerializeField]
        private TeamBehaviourService _teamService;

        [Required]
        [SerializeField]
        private LagCompensationGateway _lagCompensation;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_projectilePool).As<IProjectilePoolService>();
            builder.RegisterComponent(_teamService).As<ITeamBehaviourService>();
            builder.RegisterComponent(_lagCompensation).As<ILagCompensationGateway>();

            builder.Register<IMazeGenerator, DFSGenerator>(Lifetime.Singleton);
            builder.Register<ITilemapGenerator, TilemapGenerator>(Lifetime.Singleton);

            builder.Register<IItemDatabase, SharedItemDatabase>(Lifetime.Singleton);
            builder.Register<IMessageBus, MessageBus>(Lifetime.Singleton);
            builder.Register<IWeaponFactory, WeaponFactory>(Lifetime.Singleton);
            builder.Register<IEntityService, EntityService>(Lifetime.Singleton);
        }
    }
}
