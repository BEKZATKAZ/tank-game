using Tanks.Server.Generation;
using Tanks.Server.Generation.Crates;
using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Playground;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Playground
{
    public sealed class PlaygroundCrateSpawner : NetworkComponent<IPlaygroundCrateSpawn>
    {
        public PlaygroundCrateSpawner(ICrateSpawner spawner)
        {
            _spawner = spawner;
        }

        private readonly ICrateSpawner _spawner;
        private byte _remainingAttemptsToSpawn = 1;

        private void SpawnCrate()
        {
            int2 coords = (int2)Transform.WorldPosition;
            ICrateBehaviour crate = _spawner.Spawn(new GeneratedCrate(coords, Behaviour.Tier));
            crate.CanDropItems = Behaviour.CanDropItems;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_remainingAttemptsToSpawn <= 0) return;
            if (tick % 40 > 0) return;

            float2 center = (int2)Transform.WorldPosition + 1;
            float2 size = 1;

            if (Physics2D.OverlapBox(center, size, angle: 0))
            {
                _remainingAttemptsToSpawn = 2;
                return;
            }

            // Prevent spawning crates right after breaking
            if (--_remainingAttemptsToSpawn > 0) return;

            if (Behaviour.CanRespawn) _remainingAttemptsToSpawn = 2;

            SpawnCrate();
        }
    }
}
