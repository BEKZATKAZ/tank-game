using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Helpers;
using Tanks.Shared.Items.Data;
using Unity.Mathematics;

namespace Tanks.Server.Items.Pickups
{
    public class ItemChooser
    {
        public ItemChooser(
            IEnumerable<IItemConfig> items,
            Dictionary<ItemTier, PositiveFloat> chanceWeights,
            uint seed)
        {
            _random = Random.CreateFromIndex(seed);
            _chanceWeights = new Dictionary<ItemTier, PositiveFloat>(chanceWeights);

            _byTier = items
                .GroupBy(x => x.Tier)
                .ToDictionary(k => k.Key, v => v.ToArray());

            foreach (ItemTier tier in System.Enum.GetValues(typeof(ItemTier)))
            {
                if (_byTier.ContainsKey(tier)) continue;

                // Prevent randomly spawning items with unexisting tiers
                _chanceWeights.Remove(tier);

                UnityEngine.Debug.LogFormat("No items were registered with tier '{0}'", tier);
            }

            // Calculate after removing missing tiers
            _totalChance = _chanceWeights.Values.Sum(x => x);
        }

        private readonly Dictionary<ItemTier, PositiveFloat> _chanceWeights;
        private readonly Dictionary<ItemTier, IItemConfig[]> _byTier;
        private readonly Random _random;
        private readonly float _totalChance;

        public IItemConfig ChooseRandomItem(PositiveFloatOrZero alpha)
        {
            float roll = math.min(alpha * _totalChance, _totalChance - 0.00001f);
            float accumulated = 0;
            ItemTier selectedTier = default;

            foreach (var (tier, chance) in _chanceWeights)
            {
                accumulated += chance;
                if (roll >= accumulated) continue;

                selectedTier = tier;
                break;
            }

            if (_byTier.TryGetValue(selectedTier, out var items) == false || items.Length == 0)
            {
                throw new System.InvalidOperationException(
                    $"No items registered for tier '{selectedTier}'");
            }

            return items[_random.NextInt(items.Length)];
        }
    }
}
