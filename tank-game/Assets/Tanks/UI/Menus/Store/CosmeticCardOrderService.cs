namespace Tanks.UI.Menus.Store
{
    public static class CosmeticCardOrderService
    {
        public static int Compare(CosmeticCardData left, CosmeticCardData right)
        {
            int owned = right.IsOwned.CompareTo(left.IsOwned);
            if (owned != 0) return owned;

            (int group, int price) leftPrice = GetPriceOrder(left);
            (int group, int price) rightPrice = GetPriceOrder(right);
            int currency = leftPrice.group.CompareTo(rightPrice.group);
            if (currency != 0) return currency;

            int price = leftPrice.price.CompareTo(rightPrice.price);
            return price != 0 ? price : string.CompareOrdinal(left.Config.Identifier,
                right.Config.Identifier);
        }

        private static (int group, int price) GetPriceOrder(CosmeticCardData item)
        {
            if (item.Config.Identifier is "v3-body" or "v3-turret") return (0, 0);

            foreach (PurchaseOption price in item.Prices)
            {
                if (price.Currency == "parts") return (0, price.Price);
            }

            foreach (PurchaseOption price in item.Prices)
            {
                if (price.Currency == "pride") return (1, price.Price);
            }

            return (2, 0);
        }

    }
}
