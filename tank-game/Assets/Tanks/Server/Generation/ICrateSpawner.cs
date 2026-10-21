using System;
using Tanks.Server.Generation.Crates;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;

namespace Tanks.Server.Generation
{
    public interface ICrateSpawner
    {
        ICrateBehaviour Spawn(GeneratedCrate crate);
        void Spawn(ReadOnlySpan<GeneratedTile> tiles, uint seed);

        void Return(ICrateBehaviour behaviour);
        void Clear();
    }
}
