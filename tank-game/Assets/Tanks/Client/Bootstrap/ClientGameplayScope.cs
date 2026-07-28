using Tanks.Audio;
using Tanks.Client.Input;
using Tanks.Client.Items;
using Tanks.Client.Rendering;
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
            builder.RegisterAudio(Lifetime.Scoped);
            builder.RegisterLocalization(Lifetime.Scoped);

            builder.Register<
                IInputService<ManualDriveInput>,
                ManualMovementInputService>(Lifetime.Scoped);

            builder.Register<
                IInputService<TurretInput>,
                TurretInputService>(Lifetime.Scoped);

            builder.Register<ICameraMainService, CameraMainService>(Lifetime.Scoped);
            builder.Register<IShotIdGenerator, ShotIdGenerator>(Lifetime.Scoped);
            builder.Register<IClientItemDatabase, ClientItemDatabase>(Lifetime.Scoped);

            builder.RegisterComponentOnNewGameObject<CameraFollowService>(Lifetime.Scoped)
                .As<ICameraFollowService>();
        }
    }
}
