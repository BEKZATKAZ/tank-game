using NUnit.Framework;
using Tanks.Client.Visuals;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Visuals
{
    public sealed class StressServiceTests
    {
        [TestCase(0.1f)]
        [TestCase(0.3f)]
        [TestCase(0.5f)]
        [TestCase(0.9f)]
        public void AddStress_ClampsAt1(float amount)
        {
            IStressService service = new StressService();

            service.AddStress(amount);
            Assert.AreEqual(amount, service.Stress, delta: 0.01f);

            service.AddStress(amount);
            Assert.AreEqual(math.min(amount * 2, 1), service.Stress, delta: 0.01f);
        }

        [Test]
        public void ResetStress_DropsToZero()
        {
            IStressService service = new StressService();

            service.AddStress(0.6f);
            service.ResetStress();

            Assert.AreEqual(0, service.Stress);
        }

        [Test]
        public void SetMinStress_AddsMinThreshold()
        {
            IStressService service = new StressService();

            service.SetMinStress(0.4f);
            Assert.AreEqual(0.4f, service.Stress);

            service.SetMinStress(0.35f);
            Assert.AreEqual(0.4f, service.Stress);

            service.SetMinStress(0.8f);
            Assert.AreEqual(0.8f, service.Stress);

            service.SetMinStress(1.2f);
            Assert.AreEqual(1, service.Stress);
        }
    }
}
