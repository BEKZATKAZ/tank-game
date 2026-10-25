using Tanks.UI.Menus.Settings.Appliers;
using VContainer;

namespace Tanks.UI.Menus.Settings
{
    public static class SettingsRegistration
    {
        public static void RegisterSettings(this IContainerBuilder builder)
        {
            builder.Register<SettingsViewModel>(Lifetime.Transient);
            builder.Register<GeneralSettingsApplier>(Lifetime.Singleton);
            builder.Register<GraphicsSettingsApplier>(Lifetime.Singleton);
            builder.Register<AudioSettingsApplier>(Lifetime.Singleton);

            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<GeneralSettingsApplier>();
                resolver.Resolve<GraphicsSettingsApplier>();
                resolver.Resolve<AudioSettingsApplier>();
            });
        }
    }
}
