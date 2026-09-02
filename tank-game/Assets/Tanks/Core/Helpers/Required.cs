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

        public static bool operator ==(T lhs, Required<T> rhs) => lhs == rhs.Value;
        public static bool operator !=(T lhs, Required<T> rhs) => lhs != rhs.Value;

        public static bool operator ==(Required<T> lhs, T rhs) => lhs.Value == rhs;
        public static bool operator !=(Required<T> lhs, T rhs) => lhs.Value != rhs;

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();

        public override bool Equals(object obj) => obj switch
        {
            T value => Equals(value),
            Required<T> required => Equals(required),
            _ => false
        };

        public bool Equals(T other) => Value.Equals(other);
        public bool Equals(Required<T> other) => Value.Equals(other.Value);
    }

    public static class RequiredExtensions
    {
        public static Required<T> AsRequired<T>(this T obj) where T : class
        {
            return new Required<T>(obj);
        }
    }
}
