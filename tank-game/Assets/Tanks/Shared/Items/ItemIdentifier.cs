using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Netcode;

namespace Tanks.Shared.Items
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
    public readonly struct ItemIdentifier : IEquatable<ItemIdentifier>, INetworkSerializeByMemcpy
    {
        public ItemIdentifier(ReadOnlySpan<char> text)
        {
            if (text.Length > 30)
            {
                throw new ArgumentOutOfRangeException(nameof(text), "Too large string");
            }

            Content = default;

            Span<FixedBytes30> content = MemoryMarshal.CreateSpan(ref Content, 1);
            Span<byte> contentBytes = MemoryMarshal.AsBytes(content);

            Length = (ushort)Encoding.ASCII.GetBytes(text, contentBytes);
        }

        public readonly FixedBytes30 Content;
        public readonly ushort Length;

        public char this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref FixedBytes30 contentRef = ref Unsafe.AsRef(in Content);

                ref byte firstByte = ref Unsafe.As<FixedBytes30, byte>(ref contentRef);
                ref byte targetByte = ref Unsafe.Add(ref firstByte, index);

                return Unsafe.As<byte, char>(ref targetByte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsBytes()
        {
            return MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.AsRef(in Content), 1)
            )[..Length];
        }

        public static bool operator ==(in ItemIdentifier lhs, in ItemIdentifier rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(in ItemIdentifier lhs, in ItemIdentifier rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode() => HashCode.Combine(Length, Content);
        public override bool Equals(object obj) => obj is ItemIdentifier id && Equals(id);

        public override string ToString()
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(
                    ref Unsafe.AsRef(in Content), 1))[..Length];

            return Encoding.ASCII.GetString(bytes);
        }

        public bool Equals(ItemIdentifier other)
        {
            if (Length != other.Length) return false;
            return AsBytes().SequenceEqual(other.AsBytes());
        }
    }
}
