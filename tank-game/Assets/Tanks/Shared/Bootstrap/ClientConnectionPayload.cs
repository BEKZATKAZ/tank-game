using Tanks.Core.Players;

namespace Tanks.Shared.Bootstrap
{
    public struct ClientConnectionPayload
    {
        public string PlayerId;
        public bool IsDirectSceneTest;
        public string DisplayName;
        public EquippedCosmetics Equipped;
    }
}
