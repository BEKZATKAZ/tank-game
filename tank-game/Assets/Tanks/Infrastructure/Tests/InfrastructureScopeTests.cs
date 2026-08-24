using NUnit.Framework;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using VContainer;

namespace Tanks.Infrastructure.Tests
{
    public class InfrastructureScopeTests
    {
        [Test]
        public void Register_AddsEverythingNeeded()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInfrastructure();
            IObjectResolver container = builder.Build();

            Assert.True(container.TryResolve<IFileSystem>(out _), "Invalid file system");
            Assert.True(container.TryResolve<IResourceLoader>(out _), "Invalid resource loader");

            Assert.True(container.TryResolve<ISettingsLoader>(out _), "Invalid settings loader");
            Assert.True(container.TryResolve<ISettingsService>(out _), "Invalid settings service");
        }
    }
}
