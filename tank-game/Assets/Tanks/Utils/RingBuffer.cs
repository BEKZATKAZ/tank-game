using System;
using Unity.Collections;

namespace Tanks.Utils
{
    public sealed class RingBuffer<T> : IDisposable where T : unmanaged
    {
        public RingBuffer(int size)
        {
            _buffer = new NativeArray<T>(size, Allocator.Persistent);
        }

        private readonly NativeArray<T> _buffer;

        public ref T this[int index]
        {
            get => ref _buffer.AsSpan()[index % _buffer.Length];
        }

        public void Dispose()
        {
            if (_buffer.IsCreated == false) return;
            _buffer.Dispose();
        }
    }
}
