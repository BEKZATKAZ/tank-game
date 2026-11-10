using System;
using Tanks.UI.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticPurchaseButton : CustomButton
    {
        public CosmeticPurchaseButton(
            string identifier,
            string currency,
            Action<string, string> purchase)
        {
            viewDataKey = identifier + ":" + currency;
            AddToClassList("purchase-button");
            VisualElement icon = new() { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("currency-icon");
            icon.AddToClassList("currency-icon--" + currency);
            Add(icon);
            _original = new Label { pickingMode = PickingMode.Ignore, enableRichText =
                true };

            _original.AddToClassList("purchase-original-price");
            Add(_original);
            _current = new Label { pickingMode = PickingMode.Ignore };
            _current.AddToClassList("purchase-price");
            Add(_current);
            clicked += () => purchase(identifier, currency);
        }

        private readonly Label _original;
        private readonly Label _current;

        public void Bind(PurchaseOption price)
        {
            tooltip = price.CurrencyName;
            _original.text = $"<s>{price.OriginalPrice:N0}</s>";
            _original.style.display = price.OriginalPrice == price.Price
                ? DisplayStyle.None : DisplayStyle.Flex;

            _current.text = price.Price.ToString("N0");
            SetEnabled(price.CanAfford);
        }
    }
}
