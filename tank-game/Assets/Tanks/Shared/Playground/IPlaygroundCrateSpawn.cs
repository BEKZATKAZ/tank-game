using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Playground
{
    public interface IPlaygroundCrateSpawn : INetworkBehaviourExtended
    {
        ItemTier Tier { get; }
        bool CanRespawn { get; }
        bool CanDropItems { get; }
    }
}
