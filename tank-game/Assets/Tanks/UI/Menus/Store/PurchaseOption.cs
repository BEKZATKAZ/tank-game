using Tanks.Core.Cosmetics;

namespace Tanks.UI.Menus.Store
{
    public struct PurchaseOption
    {
        public string Currency;
        public string CurrencyName;
        public int OriginalPrice;
        public int Price;
        public bool CanAfford;
    }
}
