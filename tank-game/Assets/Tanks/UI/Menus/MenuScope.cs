using Tanks.Audio;
using Tanks.Localization;
using Tanks.UI.Menus.Settings;
using Tanks.UI.Menus.Settings.Appliers;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Menus
{
    public sealed class MenuScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterAudio(Lifetime.Singleton);
            builder.RegisterLocalization(Lifetime.Singleton);
            builder.Register<SettingsViewModel>(Lifetime.Transient);

            builder.Register<GeneralSettingsApplier>(Lifetime.Singleton);
            builder.Register<GraphicsSettingsApplier>(Lifetime.Singleton);
            builder.Register<AudioSettingsApplier>(Lifetime.Singleton);

            builder.RegisterBuildCallback(resolver =>
            {
                // Create appliers
                resolver.Resolve<GeneralSettingsApplier>();
                resolver.Resolve<GraphicsSettingsApplier>();
                resolver.Resolve<AudioSettingsApplier>();
            });
        }
    }
}