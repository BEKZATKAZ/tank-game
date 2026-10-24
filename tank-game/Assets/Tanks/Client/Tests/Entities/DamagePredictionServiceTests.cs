using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities.Health;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Tests.Entities
{
    public class DamagePredictionTests
    {
        private DamagePredictionService _service;
        private IEntityBehaviour _target;
        private IEntityBehaviour _from;

        [SetUp]
        public void Setup()
        {
            _service = new DamagePredictionService();

            _target = Substitute.For<IEntityBehaviour>();
            _target.IsValid.Returns(true);

            _from = Substitute.For<IEntityBehaviour>();
            _from.IsValid.Returns(true);
        }

        [TestCase(1u)]
        [TestCase(10u)]
        [TestCase(100u)]
        public void Damage_PredictsDamage(uint amount)
        {
            HealthEventId eventId = HealthEventId.Create(GetType(), 123);

            _service.Damage(amount, _target, _from, eventId, includeStats: false);

            IPredictedHealth health = _target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IPredictedHealth>();

            health.Received(1).Damage(amount, _from, eventId);
        }

        [TestCase(1u)]
        [TestCase(10u)]
        [TestCase(100u)]
        public void Heal_PredictsHeal(uint amount)
        {
            _service.Heal(amount, _target);

            IPredictedHealth health = _target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IPredictedHealth>();

            health.Received(1).Heal(amount, null);
        }
    }
}
