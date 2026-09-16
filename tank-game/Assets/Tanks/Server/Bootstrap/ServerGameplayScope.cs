using NaughtyAttributes;
using Tanks.Shared.Bootstrap;
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

            builder.RegisterComponent(_lifetime).As<IServerLifetimeService>();

            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<IVirtualMachineService>().Start();
            });
        }
    }
}
