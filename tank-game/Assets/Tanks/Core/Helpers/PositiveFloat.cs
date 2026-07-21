using System;
using System.Globalization;

namespace Tanks.Core.Helpers
{
    public readonly struct PositiveFloat : IEquatable<PositiveFloat>, IEquatable<float>
    {
        public PositiveFloat(float value)
        {
            Value = value <= 0
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : value;
        }

        public readonly float Value;

        public static implicit operator PositiveFloat(float value) => new(value);
        public static implicit operator float(PositiveFloat value) => value.Value;

        public static bool operator ==(PositiveFloat lhs, float rhs) => lhs.Value == rhs;
        public static bool operator !=(PositiveFloat lhs, float rhs) => lhs.Value != rhs;

        public static bool operator ==(float lhs, PositiveFloat rhs) => lhs == rhs.Value;
        public static bool operator !=(float lhs, PositiveFloat rhs) => lhs != rhs.Value;

        public string ToString(string format)
        {
            return Value.ToString(format, CultureInfo.InvariantCulture);
        }

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj switch
        {
            PositiveFloat other => Equals(other),
            float other => Equals(other),
            _ => false
        };

        public bool Equals(float other) => Value == other;
        public bool Equals(PositiveFloat other) => Value == other.Value;
    }

    public readonly struct PositiveFloatOrZero : IEquatable<PositiveFloatOrZero>, IEquatable<float>
    {
        public PositiveFloatOrZero(float value)
        {
            Value = value < 0
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : value;
        }

        public readonly float Value;

        public static implicit operator PositiveFloatOrZero(float value) => new(value);
        public static implicit operator float(PositiveFloatOrZero value) => value.Value;

        public static bool operator ==(PositiveFloatOrZero lhs, float rhs) => lhs.Value == rhs;
        public static bool operator !=(PositiveFloatOrZero lhs, float rhs) => lhs.Value != rhs;

        public static bool operator ==(float lhs, PositiveFloatOrZero rhs) => lhs == rhs.Value;
        public static bool operator !=(float lhs, PositiveFloatOrZero rhs) => lhs != rhs.Value;

        public string ToString(string format)
        {
            return Value.ToString(format, CultureInfo.InvariantCulture);
        }

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj switch
        {
            PositiveFloatOrZero other => Equals(other),
            float other => Equals(other),
            _ => false
        };

        public bool Equals(float other) => Value == other;
        public bool Equals(PositiveFloatOrZero other) => Value == other.Value;
    }
}
