using System;
using System.Buffers;

namespace Tanks.Utils
{
    public readonly struct TemporaryArrayResult<T> : IDisposable
    {
        public TemporaryArrayResult(int length)
        {
            T[] rented = ArrayPool<T>.Shared.Rent(length);
            Value = new ArraySegment<T>(rented, 0, length);
        }

        public readonly ArraySegment<T> Value;

        public readonly ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= Value.Count) throw new IndexOutOfRangeException();
                return ref Value.AsSpan()[index];
            }
        }

        public void Dispose() => ArrayPool<T>.Shared.Return(Value.Array);
    }
}
