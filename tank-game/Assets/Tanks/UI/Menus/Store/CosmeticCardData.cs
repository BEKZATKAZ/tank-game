using Tanks.Core.Cosmetics;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardData
    {
        public int Revision;
        public CosmeticConfig Config;
        public CosmeticConfig Body;
        public CosmeticConfig Turret;
        public string Title;
        public bool IsOwned;
        public bool IsEquipped;
        public int PercentOff;
        public PurchaseOption[] Prices;
    }
}
