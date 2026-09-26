using System;
using System.Collections.Generic;
using Tanks.Utils;
using UnityEngine.Pool;

namespace Tanks.Server.Lag
{
    public sealed class SnapshotBuffer<TId, TSnapshot> : IDisposable
        where TId : unmanaged, IEquatable<TId>
        where TSnapshot : unmanaged
    {
        public SnapshotBuffer(int initialPoolSize, int ringBufferSize)
        {
            _bufferPool = new ObjectPool<RingBuffer<TSnapshot>>(
                createFunc: () => new RingBuffer<TSnapshot>(_ringBufferSize),
                actionOnDestroy: x => x.Dispose(),
                defaultCapacity: initialPoolSize);

            _buffers = new Dictionary<TId, RingBuffer<TSnapshot>>();
            _ringBufferSize = ringBufferSize;

            using var warmup = new TemporaryArrayResult<RingBuffer<TSnapshot>>(initialPoolSize);

            for (int i = 0; i < initialPoolSize; i++) warmup[i] = _bufferPool.Get();
            for (int i = 0; i < initialPoolSize; i++) _bufferPool.Release(warmup[i]);
        }

        private readonly ObjectPool<RingBuffer<TSnapshot>> _bufferPool;
        private readonly Dictionary<TId, RingBuffer<TSnapshot>> _buffers;
        private readonly int _ringBufferSize;

        public void Dispose()
        {
            Clear();
            _bufferPool.Dispose();
        }

        public bool TryGet(TId id, int tick, out TSnapshot snapshot)
        {
            snapshot = default;
            if (_buffers.TryGetValue(id, out RingBuffer<TSnapshot> buffer) == false) return false;

            snapshot = buffer[tick];
            return true;
        }

        public void Add(TId id, int tick, in TSnapshot snapshot)
        {
            if (_buffers.TryGetValue(id, out RingBuffer<TSnapshot> buffer) == false)
            {
                buffer = _bufferPool.Get();
                _buffers.Add(id, buffer);
            }

            buffer[tick] = snapshot;
        }

        public void Clear()
        {
            foreach (var buffer in _buffers) _bufferPool.Release(buffer.Value);
            _buffers.Clear();
        }
    }
}
