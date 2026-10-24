using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Tests.Entities
{
    public class HealthManagerTests
    {
        private AuthoritativeHealth _component;

        [SetUp]
        public void Setup()
        {
            IHealthBehaviour behaviour = Substitute.For<IHealthBehaviour>();
            behaviour.InitialHealth.Returns(100u);

            _component = new AuthoritativeHealth();
            _component.Initialize(behaviour);
        }

        [TestCase(1u)]
        [TestCase(45u)]
        [TestCase(90u)]
        public void Damage_ReducesHP(uint amount)
        {
            uint max = _component.GetState().Max;
            _component.Damage(amount);

            HealthState expected = new HealthState(100 - (int)amount, max);
            HealthState actual = _component.GetState();

            Assert.AreEqual(expected, actual, "Wrong state");
            _component.Tick(1, 0.1f);
            _component.Behaviour.Received(1).UpdateStateEveryoneRpc(actual);
        }

        [TestCase(1)]
        [TestCase(45)]
        [TestCase(90)]
        [TestCase(1000)]
        public void Heal_RestoresHPUpToMax(int amount)
        {
            _component.SetState(new HealthState(50, 150));
            _component.Heal((uint)amount);

            HealthState expected = new HealthState(50 + amount, 150);
            HealthState actual = _component.GetState();

            Assert.AreEqual(expected, actual, "Wrong state");
            _component.Tick(5, 0.1f);
            _component.Behaviour.Received(1).UpdateStateEveryoneRpc(actual);
        }
    }
}
