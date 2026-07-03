using NUnit.Framework;
using Tanks.Shared.Health;

namespace Tanks.Shared.Tests
{
    public sealed class DamageTests
    {
        [TestCase(50, 10)]
        [TestCase(1, 0)]
        [TestCase(100, 200)]
        public void NearFar_EvaluatesCorrectly(float near, float far)
        {
            IDamage damage = new NearFarDamage(
                near,
                far,
                minDistance: 2,
                maxDistance: 10);

            Assert.AreEqual(near, damage.Evaluate(distance: 0),
                "Too small distance = Near damage");

            Assert.AreEqual(near, damage.Evaluate(distance: 1.5f),
                "Too small distance = Near damage");

            Assert.AreEqual((near + far) * 0.5f, damage.Evaluate(distance: 6),
                "Should be the perfect middle");

            Assert.AreEqual(far, damage.Evaluate(distance: 10),
                "Should be the perfect far damage");

            Assert.AreEqual(far, damage.Evaluate(distance: 100),
                "Too much distance = Far damage");
        }
    }
}