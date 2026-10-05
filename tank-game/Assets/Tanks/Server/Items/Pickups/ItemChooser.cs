using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Items.Data;

namespace Tanks.Server.Items.Pickups
{
    public sealed class ItemChooser
    {
        public ItemChooser(
            IEnumerable<IItemConfig> items,
            IReadOnlyDictionary<ItemTier, int> itemCounts,
            int seed)
        {
            _random = new Random(seed);
            _itemCounts = itemCounts;

            _byTier = items
                .GroupBy(x => x.Tier)
                .ToDictionary(k => k.Key, v => v.ToArray());
        }

        private readonly IReadOnlyDictionary<ItemTier, int> _itemCounts;
        private readonly IReadOnlyDictionary<ItemTier, IItemConfig[]> _byTier;
        private readonly Queue<IItemConfig> _queue = new();
        private readonly Random _random;

        public IItemConfig ChooseNext()
        {
            if (_queue.Count == 0) CreateBalancedQueue();
            return _queue.Dequeue();
        }

        private void CreateBalancedQueue()
        {
            int totalCount = _itemCounts.Sum(x => x.Value);

            // Storing tuples since spans don't allow reference types
            // Using Span to minimize GC usage
            Span<(ItemTier, int)> buffer = stackalloc (ItemTier, int)[totalCount];

            FillBuffer(buffer);
            ShuffleBuffer(buffer);

            foreach ((ItemTier tier, int index) in buffer)
            {
                _queue.Enqueue(_byTier[tier][index]);
            }
        }

        private void FillBuffer(Span<(ItemTier, int)> buffer)
        {
            int totalIndex = 0;

            foreach ((ItemTier tier, int count) in _itemCounts)
            {
                IItemConfig[] itemsPerTier = _byTier[tier];

                for (int i = 0; i < count; i++)
                {
                    int index = _random.Next(itemsPerTier.Length);
                    buffer[totalIndex++] = (tier, index);
                }
            }
        }

        private void ShuffleBuffer(Span<(ItemTier, int)> buffer)
        {
            for (int i = buffer.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
        }
    }
}
