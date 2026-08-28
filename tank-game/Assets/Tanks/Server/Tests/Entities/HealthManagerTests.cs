using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;

namespace Tanks.Server.Tests.Entities
{
    public class HealthManagerTests
    {
        private HealthManager _component;

        [SetUp]
        public void Setup()
        {
            _component = new HealthManager();
            _component.Initialize(Substitute.For<IHealthBehaviour>());

            _component.Behaviour.Value.Returns(100);
            _component.Behaviour.MaxHealth.Returns(100);
        }

        [TestCase(1)]
        [TestCase(45)]
        [TestCase(90)]
        public void Damage_ReducesHP(float amount)
        {
            _component.Handle(new DamageTaken { Amount = amount });
            _component.Behaviour.Received(1).SetCurrentHealth(100 - amount);
        }

        [TestCase(1)]
        [TestCase(45)]
        [TestCase(90)]
        [TestCase(1000)]
        public void Heal_RestoresHPUpToMax(float amount)
        {
            _component.Behaviour.Value.Returns(50);
            _component.Behaviour.MaxHealth.Returns(150);

            _component.Handle(new HPHealed { Amount = amount });
            _component.Behaviour.Received(1).SetCurrentHealth(math.min(50 + amount, 150));
        }
    }
}