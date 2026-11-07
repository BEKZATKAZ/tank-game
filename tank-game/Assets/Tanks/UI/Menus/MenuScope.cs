using Tanks.Audio;
using Tanks.Client.Rendering;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Loading;
using Tanks.UI.Menus.LobbyRoom;
using Tanks.UI.Menus.LobbyBrowser;
using Tanks.UI.Menus.Main;
using Tanks.UI.Menus.Play;
using Tanks.UI.Menus.Sections;
using Tanks.UI.Menus.Settings;
using Tanks.UI.Menus.TankModel;
using Tanks.UI.Menus.Transitions;
using Tanks.UI.TankModel;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Tanks.UI.Menus.Extras;
using Tanks.UI.Menus.Leaderboards;
using Tanks.UI.Menus.Items;

namespace Tanks.UI.Menus
{
    public sealed class MenuScope : LifetimeScope
    {
        [SerializeField] private MenuSectionService _sectionService;
        [SerializeField] private LoadingScreenService _loadingService;
        [SerializeField] private DialogScreenService _dialogService;
        [SerializeField] private TankModelService _tankModelService;
        [SerializeField] private CameraFollowService _cameraFollow;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterAudio(Lifetime.Singleton);
            builder.RegisterLocalization(Lifetime.Singleton);

            builder.RegisterComponent(_sectionService).As<IMenuSectionService>();
            builder.RegisterComponent(_loadingService).As<ILoadingScreenService>();
            builder.RegisterComponent(_dialogService).As<IDialogScreenService>();
            builder.RegisterComponent(_tankModelService).As<ITankModelService>();
            builder.RegisterComponent(_cameraFollow).As<ICameraFollowService>();

            builder.Register<FadeTransitionViewModel>(Lifetime.Transient);
            builder.Register<LoadingViewModel>(Lifetime.Transient);
            builder.Register<DialogScreenViewModel>(Lifetime.Transient);
            builder.Register<MainViewModel>(Lifetime.Transient);
            builder.Register<MenuPlayerNamesViewModel>(Lifetime.Transient);
            builder.Register<PlayViewModel>(Lifetime.Transient);
            builder.Register<ExtrasViewModel>(Lifetime.Transient);
            builder.Register<LeaderboardsViewModel>(Lifetime.Transient);
            builder.Register<ItemBrowserViewModel>(Lifetime.Transient);
            builder.Register<IItemCatalog, ItemCatalog>(Lifetime.Singleton);

            builder.RegisterSettings();
            builder.Register<ICameraMainService, CameraMainService>(Lifetime.Singleton);

            builder.Register<LobbyBrowserViewModel>(Lifetime.Transient);
            builder.Register<LobbyRoomViewModel>(Lifetime.Transient);
            builder.Register<LobbySettingsViewModel>(Lifetime.Transient);
            builder.Register<LobbyChatViewModel>(Lifetime.Transient);

            builder.RegisterBuildCallback(_ =>
            {
                // Leave the previous session completely
                if (NetworkManager.Singleton) Destroy(NetworkManager.Singleton.gameObject);
            });
        }
    }
}
