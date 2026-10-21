using Tanks.Shared.Items.Data;
using UnityEngine;

namespace Tanks.Shared.Playground
{
    public sealed class PlaygroundCrateSpawn : NetworkBehaviourExtended, IPlaygroundCrateSpawn
    {
        [SerializeField] private ItemTier _tier;
        [SerializeField] private bool _canRespawn = true;
        [SerializeField] private bool _canDropItems = true;

        public ItemTier Tier => _tier;
        public bool CanRespawn => _canRespawn;
        public bool CanDropItems => _canDropItems;
    }
}
