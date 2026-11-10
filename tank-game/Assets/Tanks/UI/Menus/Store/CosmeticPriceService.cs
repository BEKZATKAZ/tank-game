using System;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.Core.Store;
using Tanks.Localization;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticPriceService
    {
        public CosmeticPriceService(
            IStoreService store,
            ILocalPlayerService player,
            ILocalizationDatabase localization,
            IServerClock clock)
        {
            _store = store;
            _player = player;
            _localization = localization;
            _clock = clock;
        }

        private readonly IStoreService _store;
        private readonly ILocalPlayerService _player;
        private readonly ILocalizationDatabase _localization;
        private readonly IServerClock _clock;
        private readonly string[] _currencies = { "parts", "pride" };
        private readonly string[] _names = new string[2];

        public void RefreshText()
        {
            for (int i = 0; i < _currencies.Length; i++)
            {
                _localization.TryGet("Menus", $"currency.{_currencies[i]}.name",
                    out ILocalizedString text);

                _names[i] = text?.Get() ?? "";
            }
        }

        public bool Update(CosmeticCardData item, DiscountDefinition discount)
        {
            CosmeticDefinition definition = default;
            _store.Definitions.Items?.TryGetValue(item.Config.Identifier, out definition);
            int count = 0;

            foreach (string currency in _currencies)
            {
                if (HasPrice(definition, currency)) count++;
            }

            bool changed = item.Prices == null || item.Prices.Length != count;
            if (changed) item.Prices = new PurchaseOption[count];
            int index = 0;

            for (int i = 0; i < _currencies.Length; i++)
            {
                string currency = _currencies[i];
                if (HasPrice(definition, currency) == false) continue;
                int original = definition.Conditions.Prices[currency];
                int price = discount.Apply(original, _clock.UtcNow);
                PlayerBalance balance = _player.LocalPlayer.Balance.Value;
                bool affordable = _player.IsLoaded && price <=
                    (currency == "parts" ? balance.SpareParts : balance.PridePoints);

                PurchaseOption previous = item.Prices[index];
                changed |= previous.Currency != currency || previous.CurrencyName !=
                    _names[i] ||
                    previous.OriginalPrice != original || previous.Price != price ||
                    previous.CanAfford != affordable;

                item.Prices[index++] = new PurchaseOption
                {
                    Currency = currency,
                    CurrencyName = _names[i],
                    OriginalPrice = original,
                    Price = price,
                    CanAfford = affordable
                };
            }

            return changed;
        }

        private bool HasPrice(CosmeticDefinition definition, string currency)
        {
            return _store.IsCatalogLoaded && definition.Conditions.Prices != null &&
                definition.Conditions.Prices.TryGetValue(currency, out int price) &&
                    price >= 0;
        }
    }
}
