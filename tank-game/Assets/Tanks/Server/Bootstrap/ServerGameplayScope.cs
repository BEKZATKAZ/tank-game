using Tanks.Shared.Bootstrap;
using VContainer;

namespace Tanks.Server.Bootstrap
{
    public sealed class ServerGameplayScope : IGameplayScope<GameplayScope>
    {
        public void Configure(IContainerBuilder builder)
        {
            builder.Register<
                IClientConnectionValidator, 
                ClientConnectionValidator>(Lifetime.Singleton);

            builder.Register<
                IConnectedClientService,
                ConnectedClientService>(Lifetime.Singleton);
        }
    }
}
