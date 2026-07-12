using System.Runtime.CompilerServices;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Weapons
{
    public interface IShotIdGenerator
    {
        ShotId GenerateShotId(ulong clientId, int tick, int interval = 1000);
    }

    public sealed class ShotIdGenerator : IShotIdGenerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ShotId GenerateShotId(ulong clientId, int tick, int interval = 1000)
        {
            // Prevent same ids across multiple clients
            int baseId = (int)clientId * interval;
            return new ShotId(tick + baseId);
        }
    }
}
