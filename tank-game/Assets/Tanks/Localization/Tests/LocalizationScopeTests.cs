using NSubstitute;
using NUnit.Framework;
using VContainer;

namespace Tanks.Localization.Tests
{
    public class LocalizationScopeTests
    {
        [TestCase(Lifetime.Singleton)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        public void Register_AddsLocalizationDatabase(Lifetime lifetime)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterLocalization(lifetime);

            IObjectResolver container = builder.Build();
            Assert.True(container.TryResolve<ILocalizationDatabase>(out _), "Invalid database");
        }
    }
}
