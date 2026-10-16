using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Tests.Entities
{
    public class AuthoritativeDamageTests
    {
        private AuthoritativeDamageService _service;
        private IEntityBehaviour _target;
        private IEntityBehaviour _from;

        [SetUp]
        public void Setup()
        {
            _service = new AuthoritativeDamageService();

            _target = Substitute.For<IEntityBehaviour>();
            _target.IsValid.Returns(true);

            _from = Substitute.For<IEntityBehaviour>();
            _from.IsValid.Returns(true);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void Damage_RegistersDamage(float amount)
        {
            HealthEventId eventId = HealthEventId.Create(GetType(), 123);

            _service.Damage(amount, _target, _from, eventId, includeStats: false);

            IAuthoritativeHealth health = _target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            health.Received(1).Damage(amount, _from, eventId);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void Heal_RegistersHeal(float amount)
        {
            _service.Heal(amount, _target);

            IAuthoritativeHealth health = _target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            health.Received(1).Heal(amount, null);
        }
    }
}
