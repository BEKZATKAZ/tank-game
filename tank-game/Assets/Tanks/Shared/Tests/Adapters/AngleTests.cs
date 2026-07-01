using NUnit.Framework;
using System.Security.Cryptography;
using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Adapters
{
    public class AngleTests
    {
        [TestCase(0)]
        [TestCase(90)]
        [TestCase(180)]
        [TestCase(270)]
        public void Creation_IdenticalAngle_ShouldBeEqual(float deg)
        {
            float rad = math.radians(deg);

            Angle a = Angle.FromDegrees(deg);
            Angle b = Angle.FromRadians(rad);

            Assert.AreEqual(deg, a.Degrees, "FromDegrees() error");
            Assert.AreEqual(deg, b.Degrees, "FromRadians() error");

            Assert.AreEqual(rad, a.Radians, "FromDegrees() error");
            Assert.AreEqual(rad, b.Radians, "FromRadians() error");
        }

        [TestCase(0, 0)]
        [TestCase(90, math.PIHALF)]
        [TestCase(180, math.PI)]
        [TestCase(360, math.PI2)]
        public void Comparison_IdenticalAngles_ShouldReturnTrue(float deg, float rad)
        {
            Angle a = Angle.FromDegrees(deg);
            Angle b = Angle.FromRadians(rad);

            Assert.False(a != b);
            Assert.True(a == b);

            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));

            Assert.True(a.Equals((object)b));
            Assert.True(b.Equals((object)a));
        }

        [TestCase(0, 1)]
        [TestCase(90, math.PI)]
        [TestCase(180, math.PI + 0.1f)]
        [TestCase(0, 3.14f)]
        public void Comparison_DifferentAngles_ShouldReturnFalse(float deg, float rad)
        {
            Angle a = Angle.FromDegrees(deg);
            Angle b = Angle.FromRadians(rad);

            Assert.True(a != b);
            Assert.False(a == b);

            Assert.False(a.Equals(b));
            Assert.False(a.Equals((object)b));
        }

        [Test]
        public void Operations_ShouldEvaluateCorrectly()
        {
            // Addition
            Assert.AreEqual(
                Angle.FromDegrees(180),
                Angle.FromDegrees(90) + Angle.FromDegrees(90));

            // Substraction
            Assert.AreEqual(
                Angle.FromDegrees(40),
                Angle.FromDegrees(90) - Angle.FromDegrees(50));

            // Multiplication
            Assert.AreEqual(
                Angle.FromDegrees(150),
                Angle.FromDegrees(5) * Angle.FromDegrees(30));

            // Division
            Assert.AreEqual(
                Angle.FromDegrees(90),
                Angle.FromDegrees(45) / Angle.FromDegrees(0.5f));

            // Modulus
            Assert.AreEqual(
                Angle.FromDegrees(90),
                Angle.FromDegrees(270) % Angle.FromDegrees(180));
        }
    }
}