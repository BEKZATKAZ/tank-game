using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Tests.Entities
{
    public sealed class TestDamageService : DamageServiceBase
    {
        public PositiveFloat FullAmount;
        public IEntityBehaviour Target;
        public IEntityBehaviour From;
        public HealthEventId EventId;
        public int Received;

        protected override void OnDamage(
            PositiveFloat fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            Received++;
            FullAmount = fullAmount;
            Target = target;
            From = from;
            EventId = eventId;
        }

        protected override void OnHeal(PositiveFloat amount, IEntityBehaviour target)
        {
            Received++;
            FullAmount = amount;
            Target = target;
        }
    }

    public class DamageServiceTests
    {
        private TestDamageService _service;
        private IEntityBehaviour _target;
        private IStatOwnerBehaviour _targetStatOwner;
        private IEntityBehaviour _from;
        private IStatOwnerBehaviour _fromStatOwner;

        [SetUp]
        public void Setup()
        {
            _service = new TestDamageService();

            _target = Substitute.For<IEntityBehaviour>();
            _target.IsValid.Returns(true);
            _targetStatOwner = _target.GetBehaviour<IStatOwnerBehaviour>();

            _from = Substitute.For<IEntityBehaviour>();
            _from.IsValid.Returns(true);
            _fromStatOwner = _from.GetBehaviour<IStatOwnerBehaviour>();
        }

        [Test]
        public void Damage_CallsOnDamage()
        {
            for (float amount = 20; amount <= 100; amount += 20)
            {
                AssertDamageWithoutStats(amount);

                for (float bonus = 0; bonus <= 20; bonus += 2)
                {
                    for (float defense = 0; defense <= 20; defense += 2)
                    {
                        AssertDamageWithStats(amount, bonus, defense);
                    }
                }
            }
        }

        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(-99999)]
        public void Damage_NegativeDefense_DealsPositiveDamage(float defense)
        {
            AssertDamageWithStats(amount: 18, damageBonus: 1000, defense);
        }

        [Test]
        public void Heal_CallsOnHeal()
        {
            for (float amount = 20; amount <= 100; amount += 20)
            {
                _service.Received = 0;
                _service.Heal(amount, _target);

                Assert.That(_service.Received, Is.EqualTo(1));
                Assert.That(_service.Target, Is.EqualTo(_target));
                Assert.That(_service.FullAmount, Is.EqualTo(amount));
            }
        }

        private static float GetDiminishingReturns(float value, int diminishingBase)
        {
            return value switch
            {
                > 0 => diminishingBase / (diminishingBase + value),
                < 0 => 1 + (-value / diminishingBase),
                _ => 1,
            };
        }

        private void AssertDamageWithStats(PositiveFloat amount, float damageBonus, float defense)
        {
            float multiplier = GetDiminishingReturns(defense, 50);
            float fullDamage = amount + damageBonus;

            float expectedDamage = fullDamage * multiplier;
            float expectedDamageWithoutBonus = amount * multiplier;

            _fromStatOwner
                .GetStat(StatType.Damage, amount)
                .Returns(amount + damageBonus);

            _targetStatOwner
                .GetStat(StatType.Defense, 0)
                .Returns(defense);

            _targetStatOwner
                .GetDiminishingReturns(StatType.Defense, defense)
                .Returns(multiplier);

            _service.Received = 0;
            _service.Damage(amount, _target, _from, includeStats: true);

            Assert.That(_service.Received, Is.EqualTo(1));
            Assert.AreEqual(expectedDamage, _service.FullAmount, delta: 0.01f);

            Assert.That(_service.Target, Is.EqualTo(_target));
            Assert.That(_service.From, Is.EqualTo(_from));

            // With target, but no from
            _service.Damage(amount, target: _target, from: null, includeStats: true);
            Assert.That(_service.Received, Is.EqualTo(2));
            Assert.AreEqual(expectedDamageWithoutBonus, _service.FullAmount, delta: 0.01f);
        }

        private void AssertDamageWithoutStats(PositiveFloat amount)
        {
            _service.Received = 0;
            _service.Damage(amount, _target, _from, includeStats: false);

            Assert.That(_service.Received, Is.EqualTo(1));
            Assert.That(_service.FullAmount, Is.EqualTo(amount));

            Assert.That(_service.Target, Is.EqualTo(_target));
            Assert.That(_service.From, Is.EqualTo(_from));
        }
    }
}
