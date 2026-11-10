using System;
using Tanks.UI.Elements;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardActions : VisualElement
    {
        public CosmeticCardActions(
            string identifier,
            Action<string, string> purchase,
            Action<string> equip)
        {
            AddToClassList("cosmetic-actions");
            _equip = new CustomButton { viewDataKey = identifier + ":equip" };
            _equip.clicked += () => equip(identifier);
            Add(_equip);
            _purchases = new VisualElement();
            _purchases.AddToClassList("purchase-column");
            _parts = new CosmeticPurchaseButton(identifier, "parts", purchase);
            _pride = new CosmeticPurchaseButton(identifier, "pride", purchase);
            _purchases.Add(_parts);
            _purchases.Add(_pride);
            Add(_purchases);
        }

        private readonly CustomButton _equip;
        private readonly VisualElement _purchases;
        private readonly CosmeticPurchaseButton _parts;
        private readonly CosmeticPurchaseButton _pride;
        private string _equipEntry;

        public void Bind(CosmeticCardData item)
        {
            _equip.style.display = item.IsOwned ? DisplayStyle.Flex : DisplayStyle.None;
            _purchases.style.display = item.IsOwned ? DisplayStyle.None : DisplayStyle.Flex;
            _equip.SetEnabled(item.IsEquipped == false);
            string entry = item.IsEquipped ? "menu.store.equipped" : "menu.store.equip";

            if (entry != _equipEntry)
            {
                _equipEntry = entry;
                _equip.SetBinding("text", new LocalizedString("Menus", entry));
            }

            bool parts = false;
            bool pride = false;
            bool first = true;

            foreach (PurchaseOption price in item.Prices)
            {
                CosmeticPurchaseButton button = price.Currency == "parts" ? _parts : _pride;
                button.Bind(price);
                button.EnableInClassList("purchase-button--spaced", first == false);
                parts |= price.Currency == "parts";
                pride |= price.Currency == "pride";
                first = false;
            }

            _parts.style.display = parts ? DisplayStyle.Flex : DisplayStyle.None;
            _pride.style.display = pride ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public VisualElement FindFocus(string key)
        {
            if (_equip.style.display == DisplayStyle.Flex)
            {
                return IsAvailable(_equip) ? _equip : null;
            }

            if (key == _parts.viewDataKey && IsAvailable(_parts)) return _parts;
            if (key == _pride.viewDataKey && IsAvailable(_pride)) return _pride;
            return null;
        }

        private static bool IsAvailable(VisualElement element)
        {
            return element.enabledInHierarchy && element.style.display != DisplayStyle.None;
        }
    }
}
