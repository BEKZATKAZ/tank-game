using Tanks.Core.Players;

namespace Tanks.Server
{
    public sealed class ClientData
    {
        public ClientData(string displayName, EquippedCosmetics equipped = default)
        {
            DisplayName = displayName;
            Equipped = equipped;
        }

        public string DisplayName { get; }
        public EquippedCosmetics Equipped { get; }
    }
}
