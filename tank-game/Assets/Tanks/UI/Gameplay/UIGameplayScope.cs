using Tanks.UI.Gameplay.Eliminated;
using Tanks.UI.Gameplay.ItemDescription;
using Tanks.UI.Gameplay.StartCountdown;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Gameplay
{
    public sealed class UIGameplayScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<StartCountdownViewModel>(Lifetime.Transient);
            builder.Register<EliminatedViewModel>(Lifetime.Transient);
            builder.Register<ItemDescriptionViewModel>(Lifetime.Transient);
        }
    }
}
