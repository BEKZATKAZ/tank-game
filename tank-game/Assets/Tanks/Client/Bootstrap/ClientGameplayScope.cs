using NaughtyAttributes;
using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Client.Entities.Health;
using Tanks.Client.Input;
using Tanks.Client.Items;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Client.Visuals.Hitstop;
using Tanks.Client.Weapons;
using Tanks.Localization;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Unity.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientGameplayScope : GameplayScope
    {
        [Required]
        [SerializeField]
        private UnityClientItemDatabase _clientItemDatabase;

        [Required]
        [SerializeField]
        private CameraFollowService _cameraFollowService;

        [Required]
        [SerializeField]
        private UnityCameraShakeService _shakeService;

        [Required]
        [SerializeField]
        private UnityVisualService _visualService;

        protected override void Configure(IContainerBuilder builder)
        {
#if UNITY_EDITOR
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            if (role is not MultiplayerRoleFlags.Client) return;
#endif

            base.Configure(builder);

            builder.RegisterAudio(Lifetime.Singleton);
            builder.RegisterLocalization(Lifetime.Singleton);

            builder.Register<
                IInputService<ManualDriveInput>, 
                ManualMovementInputService>(Lifetime.Singleton);

            builder.Register<
                IInputService<TurretInput>, 
                TurretInputService>(Lifetime.Singleton);

            builder.Register<IClientEntityService, ClientEntityService>(Lifetime.Singleton);
            builder.Register<ICameraMainService, CameraMainService>(Lifetime.Singleton);
            builder.Register<ISpectatorService, SpectatorService>(Lifetime.Singleton);

            builder.Register<IHitstopService, HitstopService>(Lifetime.Singleton);
            builder.Register<IStressService, StressService>(Lifetime.Singleton);
            builder.Register<IShotIdGenerator, ShotIdGenerator>(Lifetime.Singleton);
            builder.Register<IDamageService, DamagePredictionService>(Lifetime.Singleton);

            builder.RegisterComponent(_clientItemDatabase).As<IClientItemDatabase>();
            builder.RegisterComponent(_shakeService).As<ICameraShakeService>();
            builder.RegisterComponent(_cameraFollowService).As<ICameraFollowService>();
            builder.RegisterComponent(_visualService).As<IVisualService>();
        }
    }
}
