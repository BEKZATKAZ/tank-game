using Tanks.UI.Menus.Settings;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Menus
{
    public sealed class MenuScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SettingsViewModel>(Lifetime.Transient);
        }
    }
}