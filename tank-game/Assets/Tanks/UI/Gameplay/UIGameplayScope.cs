using Tanks.Client.Input;
using Tanks.UI.Gameplay.Eliminated;
using Tanks.UI.Gameplay.GameStats;
using Tanks.UI.Gameplay.Input;
using Tanks.UI.Gameplay.ItemDescription;
using Tanks.UI.Gameplay.PlayerStats;
using Tanks.UI.Gameplay.StartCountdown;
using Tanks.UI.Gameplay.Timer;
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
            builder.Register<PlayerStatsViewModel>(Lifetime.Transient);
            builder.Register<GameStatsViewModel>(Lifetime.Transient);
            builder.Register<TimerViewModel>(Lifetime.Transient);

            builder.Register<
                IInputService<UIGameplayInput>,
                UIGameplayInputService>(Lifetime.Singleton);
        }
    }
}
