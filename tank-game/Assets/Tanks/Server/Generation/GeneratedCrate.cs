using Tanks.Shared.Items.Data;
using Unity.Mathematics;

namespace Tanks.Server.Generation.Crates
{
    public readonly struct GeneratedCrate
    {
        public GeneratedCrate(int2 position, ItemTier tier)
        {
            Position = position;
            Tier = tier;
        }

        // Bottom-left tile instead of the center, because the generator works on the
        // integer tile grid and the spawner knows where the prefab pivot is
        public readonly int2 Position;

        public readonly ItemTier Tier;
    }
}
