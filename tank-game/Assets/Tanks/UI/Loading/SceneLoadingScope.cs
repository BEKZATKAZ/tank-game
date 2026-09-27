using Tanks.Localization;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Loading
{
    public sealed class SceneLoadingScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<
                ILocalizationDatabase,
                UnityLocalizationDatabase>(Lifetime.Singleton);

            builder.Register<SceneLoadingViewModel>(Lifetime.Transient);
        }
    }
}
