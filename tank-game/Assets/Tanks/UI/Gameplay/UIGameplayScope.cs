using Tanks.Client.Bootstrap;
using Tanks.Client.Input;
using Tanks.Core.Common.Scenes;
using Tanks.UI.Gameplay.Eliminated;
using Tanks.UI.Gameplay.GameStats;
using Tanks.UI.Gameplay.Input;
using Tanks.UI.Gameplay.ItemDescription;
using Tanks.UI.Gameplay.ItemList;
using Tanks.UI.Gameplay.ItemTagline;
using Tanks.UI.Gameplay.Killfeed;
using Tanks.UI.Gameplay.Pause;
using Tanks.UI.Gameplay.PickupFeed;
using Tanks.UI.Gameplay.PlayerNames;
using Tanks.UI.Gameplay.PlayerStats;
using Tanks.UI.Gameplay.StartCountdown;
using Tanks.UI.Gameplay.Timer;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Settings;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Gameplay
{
    public sealed class UIGameplayScope : LifetimeScope
    {
        [SerializeField] private DialogScreenService _pauseDialog;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PauseMenuViewModel>(Lifetime.Singleton);
            builder.Register<IGameplayExitService, GameplayExitService>(Lifetime.Singleton);
            builder.Register<DialogScreenViewModel>(Lifetime.Transient);
            builder.RegisterComponent(_pauseDialog).As<IDialogScreenService>();
            builder.RegisterSettings();

            builder.Register<StartCountdownViewModel>(Lifetime.Transient);
            builder.Register<EliminatedViewModel>(Lifetime.Transient);

            builder.Register<ItemDescriptionViewModel>(Lifetime.Transient);
            builder.Register<ItemTaglineViewModel>(Lifetime.Transient);
            builder.Register<ItemListViewModel>(Lifetime.Transient);

            builder.Register<PlayerHealthViewModel>(Lifetime.Transient);
            builder.Register<PlayerNamesViewModel>(Lifetime.Transient);
            builder.Register<PlayerAmmoViewModel>(Lifetime.Transient);
            builder.Register<PlayerStatsViewModel>(Lifetime.Transient);

            builder.Register<GameStatsViewModel>(Lifetime.Transient);
            builder.Register<TimerViewModel>(Lifetime.Transient);
            builder.Register<KillfeedViewModel>(Lifetime.Transient);
            builder.Register<PickupFeedViewModel>(Lifetime.Transient);

            builder.Register<
                IInputService<UIGameplayInput>,
                UIGameplayInputService>(Lifetime.Singleton);
        }
    }
}
