using NUnit.Framework;
using Tanks.Server.Bootstrap;
using VContainer;

namespace Tanks.Server.Tests.Bootstrap
{
    public class ServerGameplayScopeTests
    {
        [Test]
        public void Configure_AddsServerService()
        {
            ServerGameplayScope scope = new ServerGameplayScope();
            ContainerBuilder builder = new ContainerBuilder();

            scope.Configure(builder);
            IObjectResolver container = builder.Build();

            Assert.True(container.TryResolve(out IConnectedClientService _));
            Assert.True(container.TryResolve(out IClientConnectionValidator _));
        }
    }
}
