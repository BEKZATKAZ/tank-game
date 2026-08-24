using NUnit.Framework;
using System;
using System.Globalization;
using Tanks.Core.Helpers;

namespace Tanks.Core.Tests.Helpers
{
    public class PositiveFloatTests
    {
        [TestCase(float.Epsilon)]
        [TestCase(1)]
        [TestCase(2.5f)]
        [TestCase(float.MaxValue - 10)]
        [TestCase(float.PositiveInfinity)]
        public void Positive_Passes(float value)
        {
            Assert.DoesNotThrow(() => new PositiveFloat(value),
                "Should not throw because the value is positive!");

            Assert.Throws<ArgumentOutOfRangeException>(() => new PositiveFloat(-value),
                "Should throw because the value is negative!");
        }

        [TestCase(float.Epsilon)]
        [TestCase(1)]
        [TestCase(2.5f)]
        [TestCase(float.MaxValue - 10)]
        [TestCase(float.PositiveInfinity)]
        public void PositiveOrZero_Passes(float value)
        {
            Assert.DoesNotThrow(() => new PositiveFloatOrZero(value),
                "Should not throw because the value is positive!");

            Assert.Throws<ArgumentOutOfRangeException>(() => new PositiveFloatOrZero(-value),
                "Should throw because the value is negative!");
        }

        [Test]
        public void PositiveOrZero_ValueZero_Passes()
        {
            Assert.DoesNotThrow(() => new PositiveFloatOrZero(0),
                "Should not throw because the value is allowed to be zero!");
        }

        [Test]
        public void PositiveFloat_DefaultMethods()
        {
            PositiveFloat x = 150.45f;
            float y = 150.45f;

            Assert.AreEqual(y.ToString(), x.ToString(),
                "Invalid ToString()");

            Assert.AreEqual(y.ToString("#.#", CultureInfo.InvariantCulture), x.ToString("#.#"),
                "Invalid ToString() with format");

            Assert.AreEqual(y.GetHashCode(), x.GetHashCode(),
                "Invalid GetHashCode()");

            Assert.True(x.Equals(x), $"Invalid Equals() with PositiveFloat");
            Assert.True(x.Equals(x.Value), $"Invalid Equals() with float");
            Assert.False(x.Equals("test"), $"Invalid Equals() with unsupported type");
        }

        [Test]
        public void PositiveFloat_Equals_RunsCorrectly()
        {
            float a = 1;
            PositiveFloat b = 1;

            Assert.True(b == a);
            Assert.True(a == b);

            Assert.False(b != a);
            Assert.False(a != b);

            Assert.True(b.Equals(a));
            Assert.True(b.Equals(b));
        }

        [Test]
        public void PositiveFloatOrZero_Equals_RunsCorrectly()
        {
            float a = 1;
            PositiveFloatOrZero b = 1;

            Assert.True(b == a);
            Assert.True(a == b);

            Assert.False(b != a);
            Assert.False(a != b);

            Assert.True(b.Equals(a));
            Assert.True(b.Equals(b));
        }
    }
}
