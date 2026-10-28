using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Tests.Items
{
    public class ThornsTests : ItemBehaviourTestBase<ThornsBehaviour>
    {
        private IDamageService _damageService;

        protected override void InitializeConfig()
        {
            AddItemStat("damage", new ItemStat { Value = 25 });
        }

        protected override ThornsBehaviour CreateBehaviour()
        {
            _damageService = Substitute.For<IDamageService>();
            return new ThornsBehaviour(_damageService);
        }

        [TestCase(100, 80, 5u)]
        [TestCase(100, 99, 1u)]
        [TestCase(100, 0, 25u)]
        public void HealthChanged_Damage_ReflectsPercentageRoundedUp(int oldHealth, int newHealth, uint expected)
        {
            IEntityBehaviour attacker = Substitute.For<IEntityBehaviour>();

            Behaviour.OnHealthChanged(
                MockOwner, attacker, new HealthState(oldHealth, 100), new HealthState(newHealth, 100));

            _damageService.Received(1).Damage(expected, attacker, MockOwner.Entity, includeStats: false);
        }

        [TestCase(50)]
        [TestCase(60)]
        public void HealthChanged_HealingOrUnchangedHealth_DoesNotReflect(int health)
        {
            IEntityBehaviour attacker = Substitute.For<IEntityBehaviour>();

            Behaviour.OnHealthChanged(MockOwner, attacker, new HealthState(50, 100), new HealthState(health, 100));

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HealthChanged_SelfDamageOrMissingAttacker_DoesNotReflect(bool selfDamage)
        {
            IEntityBehaviour attacker = selfDamage ? MockOwner.Entity : null;

            Behaviour.OnHealthChanged(MockOwner, attacker, new HealthState(100, 100), new HealthState(80, 100));

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }
    }
}
