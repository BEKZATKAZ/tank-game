using System;

namespace Tanks.Core.Helpers
{
    public readonly struct Required<T> : IEquatable<Required<T>>, IEquatable<T> where T : class
    {
        public Required(T value)
        {
            Value = value is null ? throw new NullReferenceException() : value;
        }

        public readonly T Value;

        public static implicit operator Required<T>(T value) => new(value);
        public static implicit operator T(Required<T> value) => value.Value;

        public static bool operator ==(Required<T> lhs, Required<T> rhs) => lhs.Equals(rhs);
        public static bool operator !=(Required<T> lhs, Required<T> rhs) => !lhs.Equals(rhs);

        public static bool operator ==(T lhs, Required<T> rhs) => lhs.Equals(rhs.Value);
        public static bool operator !=(T lhs, Required<T> rhs) => !lhs.Equals(rhs.Value);

        public static bool operator ==(Required<T> lhs, T rhs) => lhs.Value.Equals(rhs);
        public static bool operator !=(Required<T> lhs, T rhs) => !lhs.Value.Equals(rhs);

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => Value.Equals(obj);

        public bool Equals(T other) => Value.Equals(other);
        public bool Equals(Required<T> other) => Value.Equals(other.Value);
    }
}
