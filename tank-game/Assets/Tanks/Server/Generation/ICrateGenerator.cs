using System;
using Tanks.Shared.Generation;
using Tanks.Utils;

namespace Tanks.Server.Generation.Crates
{
    public interface ICrateGenerator
    {
        TemporaryArrayResult<GeneratedCrate> Generate(
            ReadOnlySpan<GeneratedTile> tiles, uint seed, CrateGenerationSettings settings);
    }
}
