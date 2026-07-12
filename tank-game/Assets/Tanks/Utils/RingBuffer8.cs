using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tanks.Utils
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct RingBuffer8<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RingBuffer8(
            T a = default, T b = default, T c = default, T d = default,
            T e = default, T f = default, T g = default, T h = default)
        {
            A = a;
            B = b;
            C = c;
            D = d;

            E = e;
            F = f;
            G = g;
            H = h;
        }

        public int Length => 8;

        public readonly T A;
        public readonly T B;
        public readonly T C;
        public readonly T D;

        public readonly T E;
        public readonly T F;
        public readonly T G;
        public readonly T H;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index switch
            {
                0 => A,
                1 => B,
                2 => C,
                3 => D,

                4 => E,
                5 => F,
                6 => G,
                7 => H,

                _ => throw new IndexOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RingBuffer8<T> Add(T element)
        {
            return new RingBuffer8<T>(B, C, D, E, F, G, H, element);
        }
    }
}
