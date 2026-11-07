using Cysharp.Threading.Tasks;
using System;
using System.Globalization;
using System.Linq;
using Tanks.Client.Effects;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.UI.Items;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.Items
{
    public sealed class ItemBrowserViewModel : IViewModel, IDisposable
    {
        public ItemBrowserViewModel(
            IItemCatalog catalog, ILocalizationDatabase localization,
            IMenuSectionService navigation)
        {
            _catalog = catalog;
            _localization = localization;
            _navigation = navigation;
            _formatter = new ItemDescriptionFormatter(localization);
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public BindableProperty<ItemDescriptionData[]> Items { get; } = new(Array.Empty<ItemDescriptionData>());
        public BindableProperty<ItemDescriptionData> HoveredItem { get; } = new();

        private readonly IItemCatalog _catalog;
        private readonly ILocalizationDatabase _localization;
        private readonly IMenuSectionService _navigation;
        private readonly ItemDescriptionFormatter _formatter;

        private ItemCatalogEntry[] _entries = Array.Empty<ItemCatalogEntry>();
        private bool _disposed;

        public async UniTask InitializeAsync(IEffectListConfig effects)
        {
            _formatter.SetConfig(effects);
            await _localization.WaitForInitializationAsync();
            if (_disposed) return;

            var entries = await _catalog.LoadAsync();
            if (_disposed) return;

            _entries = entries.ToArray();
            Refresh();
        }

        public void Hover(ItemIdentifier id)
        {
            if (_disposed) return;
            HoveredItem.Value = Find(id);
        }

        public void Leave(ItemIdentifier id)
        {
            if (HoveredItem.Value?.Config.Identifier == id) HoveredItem.Value = null;
        }

        public void Back() => _navigation.OpenSection(MenuSectionType.Extras);

        public void Dispose()
        {
            _disposed = true;
            _localization.LocaleChanged -= OnLocaleChanged;
            HoveredItem.Value = null;
        }

        private void OnLocaleChanged(string locale) => Refresh();

        private void Refresh()
        {
            ItemIdentifier? selected = HoveredItem.Value?.Config.Identifier;
            Items.Value = _entries.Select(x => _formatter.Format(x.Config, x.ClientConfig))
                .OrderBy(x => x.Config.Tier)
                .ThenBy(x => x.Name, StringComparer.Create(CultureInfo.CurrentCulture, false))
                .ThenBy(x => x.Config.Identifier.ToString(), StringComparer.Ordinal)
                .ToArray();

            if (selected.HasValue) Hover(selected.Value);
        }

        private ItemDescriptionData Find(ItemIdentifier id)
        {
            return Items.Value.FirstOrDefault(x => x.Config.Identifier == id);
        }
    }
}
