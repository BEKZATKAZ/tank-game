using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Client.Input;
using Tanks.Client.Items;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Client.Weapons;
using Tanks.Localization;
using Tanks.Shared.Bootstrap;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientGameplayScope : IGameplayScope<GameplayScope>
    {
        public void Configure(IContainerBuilder builder)
        {
            builder.RegisterAudio(Lifetime.Singleton);
            builder.RegisterLocalization(Lifetime.Singleton);

            builder.Register<
                IInputService<ManualDriveInput>, 
                ManualMovementInputService>(Lifetime.Singleton);

            builder.Register<
                IInputService<TurretInput>, 
                TurretInputService>(Lifetime.Singleton);

            builder.Register<ICameraMainService, CameraMainService>(Lifetime.Singleton);
            builder.Register<ISpectatorService, SpectatorService>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<CameraFollowService>(Lifetime.Singleton)
                .As<ICameraFollowService>();

            builder.Register<IClientItemDatabase, ClientItemDatabase>(Lifetime.Singleton);
            builder.Register<IClientEntityService, ClientEntityService>(Lifetime.Singleton);

            builder.Register<IVisualService, VisualService>(Lifetime.Singleton);
            builder.Register<IHitstopService, HitstopService>(Lifetime.Singleton);
            builder.Register<IShotIdGenerator, ShotIdGenerator>(Lifetime.Singleton);
        }
    }
}
