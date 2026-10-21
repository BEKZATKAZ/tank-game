using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Generation;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;

namespace Tanks.Server.Items.Pickups
{
    public sealed class CrateLoot :
        NetworkComponent<ICrateBehaviour>,
        INetworkMessageHandler<Died>
    {
        public CrateLoot(
            IItemDatabase itemDatabase,
            IPickupSpawnBehaviour pickupSpawner,
            ICrateSpawner crateSpawner)
        {
            _itemDatabase = itemDatabase;
            _spawner = pickupSpawner;
            _crateSpawner = crateSpawner;
            Random = new Random();
        }

        public Random Random { get; set; }

        private readonly IItemDatabase _itemDatabase;
        private readonly IPickupSpawnBehaviour _spawner;
        private readonly ICrateSpawner _crateSpawner;

        public void Handle(Died message)
        {
            if (Behaviour.CanDropItems) SpawnLoot();
            _crateSpawner.Return(Behaviour);
        }

        private void SpawnLoot()
        {
            if (TryChooseTier(out ItemTier selectedTier) == false) return;

            IEnumerable<IItemConfig> candidates = _itemDatabase.Items
                .Where(x => x.Tier == selectedTier);

            IItemConfig loot = GetRandom(candidates);

            _spawner.SpawnEveryoneRpc(new PickupSpawned
            {
                ItemId = loot.Identifier,
                WorldPosition = Transform.WorldPosition
            });
        }

        private bool TryChooseTier(out ItemTier selectedTier)
        {
            selectedTier = default;
            List<ItemTier> successfulTiers = null;

            foreach ((ItemTier tier, float chance) in Behaviour.Chances)
            {
                if (RollChance(chance))
                {
                    successfulTiers ??= new List<ItemTier>();
                    successfulTiers.Add(tier);
                }
            }

            if (successfulTiers == null || successfulTiers.Count == 0)
            {
                return false;
            }

            int index = Random.Next(successfulTiers.Count);
            selectedTier = successfulTiers[index];
            return true;
        }

        private bool RollChance(float chance)
        {
            return (double)chance > Random.NextDouble();
        }

        public T GetRandom<T>(IEnumerable<T> source)
        {
            T result = default;
            int count = 0;

            foreach (T item in source)
            {
                count++;
                if (Random.Next(count) == 0) result = item;
            }

            if (count == 0) throw new InvalidOperationException("Sequence is empty");
            return result;
        }

        protected override void OnInitialize()
        {
            Behaviour.GetBehaviour<IHealthBehaviour>().RegisterMessageHandler(this);
        }
    }
}
