using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Client.Weapons;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class ClientGameplayScope : IGameplayScope<GameplayScope>
    {
        public void Configure(IContainerBuilder builder)
        {
            builder.Register<
                IInputService<ManualDriveInput>,
                ManualMovementInputService>(Lifetime.Scoped);

            builder.Register<
                IInputService<TurretInput>,
                TurretInputService>(Lifetime.Scoped);

            builder.Register<ICameraMainService, CameraMainService>(Lifetime.Scoped);

            builder.RegisterComponentOnNewGameObject<CameraFollowService>(Lifetime.Scoped)
                .As<ICameraFollowService>();

            builder.RegisterBuildCallback(resolver =>
            {
                IMessageBus bus = resolver.Resolve<IMessageBus>();
                bus.RegisterHandler<ProjectileRicocheted>(new ProjectileRicochetSound());
            });
        }
    }
}