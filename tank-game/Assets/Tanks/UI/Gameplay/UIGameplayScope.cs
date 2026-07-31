using Tanks.UI.Gameplay.Eliminated;
using Tanks.UI.Gameplay.ItemDescription;
using Tanks.UI.Gameplay.StartCountdown;
using VContainer;

namespace Tanks.UI.Gameplay
{
    public static class UIGameplayScope
    {
        public static void RegisterGameplayUI(this IContainerBuilder builder)
        {
            builder.Register<StartCountdownViewModel>(Lifetime.Transient);
            builder.Register<EliminatedViewModel>(Lifetime.Transient);
            builder.Register<ItemDescriptionViewModel>(Lifetime.Transient);
        }
    }
}
