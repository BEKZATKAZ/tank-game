using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.Core.Store;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Sections;
using Tanks.UI.TankModel;
using Tanks.Utils;

namespace Tanks.UI.Menus.Store
{
    public sealed class StoreViewModel : IViewModel, IDisposable
    {
        public StoreViewModel(
            ILocalPlayerService player,
            IStoreService store,
            IServerClock clock,
            CosmeticDatabase database,
            ILocalizationDatabase localization,
            IDialogScreenService dialog,
            IMenuSectionService navigation,
            ITankModelService tankModels)
        {
            _player = player;
            _store = store;
            _clock = clock;
            _localization = localization;
            _navigation = navigation;
            _cards = new CosmeticCardCatalogService(database, player, store, clock,
                localization);

            _preview = new CosmeticPreviewService(database, tankModels, player);
            _actions = new StoreActionService(player, store, dialog);
            _cards.Initialize();
            _actions.Completed += RefreshCards;
            _player.LocalPlayer.Revision.Bind(OnPlayerChanged);
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public BindableProperty<CosmeticCardData[]> Items { get; } =
            new(Array.Empty<CosmeticCardData>());

        public BindableProperty<bool> CatalogError { get; } = new();
        public BindableProperty<bool> IsCatalogLoading { get; } = new();
        public BindableProperty<CosmeticType> SelectedTab { get; } = new();
        public BindableProperty<bool> IsBusy => _actions.IsBusy;
        public IReadOnlyList<CosmeticCardData> AllItems => _cards.AllItems;

        private readonly ILocalPlayerService _player;
        private readonly IStoreService _store;
        private readonly IServerClock _clock;
        private readonly ILocalizationDatabase _localization;
        private readonly IMenuSectionService _navigation;
        private readonly CosmeticCardCatalogService _cards;
        private readonly CosmeticPreviewService _preview;
        private readonly StoreActionService _actions;
        private long _lastSecond = -1;
        private bool _disposed;

        public async Task InitializeAsync()
        {
            if (IsCatalogLoading.Value || _disposed) return;
            IsCatalogLoading.Value = true;
            CatalogError.Value = false;

            try
            {
                ResultType result = await _store.LoadCatalogAsync();
                if (_disposed) return;
                CatalogError.Value = result.IsSuccess == false;
                RefreshCards();
            }
            finally
            {
                IsCatalogLoading.Value = false;
            }
        }

        public CosmeticCardData[] GetPage(CosmeticType type) => _cards.GetPage(type);

        public void SelectTab(CosmeticType type)
        {
            if (_disposed) return;
            if (_cards.HasConfigurationChanges) RefreshCards();
            if (SelectedTab.Value == type) return;
            SelectedTab.Value = type;
            Items.Value = _cards.GetPage(type);
        }

        public void Back()
        {
            _preview.Clear();
            _navigation.OpenSection(MenuSectionType.Main);
        }

        public void PreviewCosmetic(string identifier)
        {
            if (_disposed || IsBusy.Value || _player.IsLoaded == false) return;
            if (_preview.Select(identifier)) RefreshCards();
        }

        public void Tick()
        {
            long second = _clock.UtcNow.UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            if (_disposed || second == _lastSecond) return;
            _lastSecond = second;
            if (_cards.HasExpiredDiscount) RefreshCards();
        }

        public void ConfirmPurchase(string identifier, string currency)
        {
            _actions.ConfirmPurchase(Items.Value, identifier, currency);
        }

        public Task EquipAsync(string identifier) => _actions.EquipAsync(identifier);

        private void RefreshCards()
        {
            _cards.Update(_preview.GetPreview());
            CosmeticCardData[] page = _cards.GetPage(SelectedTab.Value);
            if (ReferenceEquals(page, Items.Value)) Items.ForceRefresh();
            if (ReferenceEquals(page, Items.Value) == false) Items.Value = page;
        }

        private void OnPlayerChanged(int revision)
        {
            _preview.Synchronize();
            RefreshCards();
        }

        private void OnLocaleChanged(string locale)
        {
            _cards.RefreshText();
            RefreshCards();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _actions.Dispose();
            _preview.Clear();
            _player.LocalPlayer.Revision.Unbind(OnPlayerChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
        }
    }
}
