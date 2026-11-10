using System;
using System.Collections.Generic;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.Core.Store;
using Tanks.Localization;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardCatalogService
    {
        public CosmeticCardCatalogService(
            CosmeticDatabase database,
            ILocalPlayerService player,
            IStoreService store,
            IServerClock clock,
            ILocalizationDatabase localization)
        {
            _database = database;
            _player = player;
            _store = store;
            _clock = clock;
            _localization = localization;
        }

        public IReadOnlyList<CosmeticCardData> AllItems => _rows;
        public bool HasExpiredDiscount => _nextExpiry <= _clock.UtcNow;
        public bool HasConfigurationChanges => _sourceCount != _database.Cosmetics.Count;

        private readonly CosmeticDatabase _database;
        private readonly ILocalPlayerService _player;
        private readonly IStoreService _store;
        private readonly IServerClock _clock;
        private readonly ILocalizationDatabase _localization;
        private readonly List<CosmeticCardData> _rows = new();
        private readonly List<CosmeticCardData> _working = new();
        private readonly Dictionary<CosmeticType, CosmeticCardData[]> _pages = new();
        private CosmeticPriceService _prices;
        private int _sourceCount;
        private DateTimeOffset _nextExpiry = DateTimeOffset.MaxValue;

        public void Initialize()
        {
            _prices = new CosmeticPriceService(_store, _player, _localization, _clock);

            foreach (CosmeticConfig config in _database.Cosmetics)
            {
                if (config) _rows.Add(new CosmeticCardData { Config = config });
            }

            _sourceCount = _database.Cosmetics.Count;
            RefreshText();
        }

        public void RefreshText()
        {
            _prices.RefreshText();
            _localization.TryGet("Menus", "menu.store.black", out ILocalizedString black);

            foreach (CosmeticCardData row in _rows)
            {
                int separator = row.Config.name.LastIndexOf('_');
                string title = separator >= 0
                    ? row.Config.name.Substring(0, separator) : row.Config.name;

                row.Title = title.Replace('_', ' ').Replace("Black", black?.Get() ?? "");
                row.Revision++;
            }
        }

        public CosmeticCardData[] GetPage(CosmeticType type) => _pages[type];


        public void Update(EquippedCosmetics preview)
        {
            if (HasConfigurationChanges)
            {
                _rows.Clear();

                foreach (CosmeticConfig config in _database.Cosmetics)
                {
                    if (config) _rows.Add(new CosmeticCardData { Config = config });
                }

                _sourceCount = _database.Cosmetics.Count;
                RefreshText();
            }

            CosmeticConfig body = _database.Find(preview.Body, CosmeticType.Body);
            CosmeticConfig turret = _database.Find(preview.Turret, CosmeticType.Turret);
            EquippedCosmetics equipped = _player.LocalPlayer.Equipped.Value;
            _nextExpiry = DateTimeOffset.MaxValue;

            foreach (CosmeticCardData row in _rows)
            {
                DiscountDefinition discount = default;
                _store.Definitions.Discounts?.TryGetValue(row.Config.Identifier, out
                    discount);

                bool active = discount.IsActive(_clock.UtcNow) && discount.PercentOff > 0;

                if (active && discount.EndDate.HasValue && discount.EndDate < _nextExpiry)
                {
                    _nextExpiry = discount.EndDate.Value;
                }

                bool owned = _player.LocalPlayer.Owns(row.Config.Identifier);
                bool selected = row.Config.Identifier == equipped.Body ||
                    row.Config.Identifier == equipped.Turret;

                CosmeticConfig nextBody = row.Config.CosmeticType == CosmeticType.Body
                    ? row.Config : body;

                CosmeticConfig nextTurret = row.Config.CosmeticType == CosmeticType.Turret
                    ? row.Config : turret;

                int percent = active ? discount.PercentOff : 0;
                bool changed = row.IsOwned != owned || row.IsEquipped != selected ||
                    row.Body != nextBody || row.Turret != nextTurret || row.PercentOff
                        != percent;

                changed |= _prices.Update(row, discount);
                row.IsOwned = owned;
                row.IsEquipped = selected;
                row.Body = nextBody;
                row.Turret = nextTurret;
                row.PercentOff = percent;
                if (changed) row.Revision++;
            }

            UpdatePage(CosmeticType.Body);
            UpdatePage(CosmeticType.Turret);
        }

        private void UpdatePage(CosmeticType type)
        {
            _working.Clear();

            foreach (CosmeticCardData row in _rows)
            {
                if (row.Config.CosmeticType == type && (row.IsOwned || row.Prices.Length
                    > 0))
                {
                    _working.Add(row);
                }
            }

            _working.Sort(CosmeticCardOrderService.Compare);

            if (_pages.TryGetValue(type, out CosmeticCardData[] previous) &&
                previous.Length == _working.Count)
            {
                int index = 0;
                while (index < previous.Length && previous[index] == _working[index])
                    index++;

                if (index == previous.Length) return;
            }

            _pages[type] = _working.ToArray();
        }
    }
}
