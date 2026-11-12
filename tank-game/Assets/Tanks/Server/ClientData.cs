using Tanks.Core.Players;

namespace Tanks.Server
{
    public sealed class ClientData
    {
        public ClientData(
            string displayName,
            EquippedCosmetics equipped = default,
            string playerId = "",
            bool isDirectSceneTest = false)
        {
            DisplayName = displayName;
            Equipped = equipped;
            PlayerId = playerId;
            IsDirectSceneTest = isDirectSceneTest;
        }

        public string DisplayName { get; }
        public bool IsDirectSceneTest { get; }
        public string PlayerId { get; }
        public EquippedCosmetics Equipped { get; }
    }
}
