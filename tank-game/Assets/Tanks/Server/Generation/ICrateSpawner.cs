using System;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;

namespace Tanks.Server.Generation
{
    public interface ICrateSpawner
    {
        void Spawn(ReadOnlySpan<GeneratedTile> tiles, uint seed);
        void Return(ICrateBehaviour behaviour);
        void Clear();
    }
}
