using Tanks.Audio;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.Lobbies;
using Tanks.UI.Menus.Main;
using Tanks.UI.Menus.Settings;
using Tanks.UI.Menus.Settings.Appliers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Menus
{
    public sealed class MenuScope : LifetimeScope
    {
        [SerializeField] private MenuNavigation _menuNavigationService;
        [SerializeField] private LoadingScreenService _loadingService;
        [SerializeField] private DialogScreenService _dialogService;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterAudio(Lifetime.Singleton);
            builder.RegisterLocalization(Lifetime.Singleton);

            builder.RegisterComponent(_menuNavigationService).As<IMenuNavigation>();
            builder.RegisterComponent(_loadingService).As<ILoadingScreenService>();
            builder.RegisterComponent(_dialogService).As<IDialogScreenService>();

            builder.Register<LoadingViewModel>(Lifetime.Transient);
            builder.Register<DialogScreenViewModel>(Lifetime.Transient);
            builder.Register<MainMenuSectionsViewModel>(Lifetime.Transient);
            builder.Register<SettingsViewModel>(Lifetime.Transient);
            builder.Register<LobbyBrowserViewModel>(Lifetime.Transient);
            builder.Register<LobbyViewModel>(Lifetime.Transient);

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