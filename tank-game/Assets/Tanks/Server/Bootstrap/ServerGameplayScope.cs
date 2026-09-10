using Tanks.Shared.Bootstrap;
using VContainer;

namespace Tanks.Server.Bootstrap
{
    public sealed class ServerGameplayScope : GameplayScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<
                IClientConnectionValidator, 
                ClientConnectionValidator>(Lifetime.Singleton);

            builder.Register<
                IConnectedClientService,
                ConnectedClientService>(Lifetime.Singleton);

            builder.Register<
                IVirtualMachineService,
                PlayFabVirtualMachineService>(Lifetime.Singleton);

            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<IVirtualMachineService>().Start();
            });
        }
    }
}
