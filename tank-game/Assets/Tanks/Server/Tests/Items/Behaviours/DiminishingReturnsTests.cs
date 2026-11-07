using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnFire;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Teams;
using Tanks.Shared.Tests.Entities;
using Tanks.Shared.Tests.Items;
using Tanks.Utils;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class DiminishingReturnsTests : ItemBehaviourTestBase<DiminishingReturnsBehaviour>
    {
        private IEntityBehaviour _target;

        protected override void InitializeConfig()
        {
            AddItemStat("threshold", new ItemStat { Value = 75 });
            AddItemStat("duration", new ItemStat { Value = 3 });
            AddItemStat("increase", new ItemStat { Value = 50 });
        }

        public override void Setup()
        {
            base.Setup();

            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id, id });
            _target = CreateTarget();
        }

        [Test]
        public void DealDamage_EverySecondEventReceivesStackedBonus()
        {
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(200));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(200));
        }

        [Test]
        public void DealDamage_DifferentTargetsShareOwnerCounter()
        {
            IEntityBehaviour other = CreateTarget();

            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, other), Is.EqualTo(200));
        }

        [Test]
        public void DealDamage_OwnersHaveIndependentCounters()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            ItemIdentifier id = Config.Identifier;
            other.Items.Returns(new[] { id });

            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
            Assert.That(Hit(other, _target), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(200));
            Assert.That(Hit(other, _target), Is.EqualTo(150));
        }

        [Test]
        public void DealDamage_SelfDamageDoesNotAdvanceCounter()
        {
            Assert.That(Hit(MockOwner, MockOwner.Entity), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
        }

        [Test]
        public void DealDamage_NonPlayerDoesNotAdvanceCounter()
        {
            IEntityBehaviour crate = CreateTarget();
            crate.GetBehaviour<ITeamParticipantBehaviour>().Returns((ITeamParticipantBehaviour)null);

            Assert.That(Hit(MockOwner, crate), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void DealDamage_DeadPlayerDoesNotAdvanceCounter(int health)
        {
            IEntityBehaviour dead = CreateTarget(health);

            Assert.That(Hit(MockOwner, dead), Is.EqualTo(100));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void DealDamage_NonPositiveDamageDoesNotAdvanceCounter(float amount)
        {
            Assert.That(Hit(MockOwner, _target, amount), Is.EqualTo(amount));
            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
        }

        [Test]
        public void Destroy_RemovesOwnerCounter()
        {
            Hit(MockOwner, _target);

            Behaviour.OnDestroy(MockOwner);

            Assert.That(Hit(MockOwner, _target), Is.EqualTo(100));
        }

        [Test]
        public void DealDamage_DamageServiceDispatchesOncePerEventAndBypassesRawDamage()
        {
            MockOwner.GetAllBehaviours().Returns(new ItemBehaviourBase[] { Behaviour, Behaviour });
            MockOwner.Entity.GetBehaviour<IItemOwnerBehaviour>().Returns(MockOwner);
            MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            _target.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            TestDamageService service = new();

            for (int hit = 1; hit <= 4; hit++)
            {
                service.Damage(100, _target, MockOwner.Entity, includeStats: false);
                Assert.That(service.FullAmount, Is.EqualTo(100));

                service.Damage(100, _target, MockOwner.Entity);
                Assert.That(service.FullAmount, Is.EqualTo(hit % 2 == 0 ? 200 : 100));
            }
        }

        [TestCase(75, 0)]
        [TestCase(76, 1)]
        public void Fire_BurnsOnlyStrictlyAboveThreshold(int health, int applications)
        {
            MockOwner.Entity.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(health, 100)));

            Behaviour.OnFire(MockOwner, default);

            MockOwner.Entity.GetBehaviour<IEffectOwnerBehaviour>().Received(applications)
                .ApplyEffect(EffectType.Burning, 6, Config.Identifier.ToString(), MockOwner.Entity);
        }

        [Test]
        public void DealDamage_DoesNotRequireOwnerServerFlag()
        {
            MockOwner.IsServer.Returns(false);
            Hit(MockOwner, _target);

            Assert.That(Hit(MockOwner, _target), Is.EqualTo(200));
        }

        private float Hit(IItemOwnerBehaviour owner, IEntityBehaviour target, float damage = 100)
        {
            Behaviour.OnDealDamage(target, owner, ref damage);
            return damage;
        }

        private static IEntityBehaviour CreateTarget(int health = 100)
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            target.IsValid.Returns(true);
            target.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(health, 100)));
            target.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(1, "target", new TeamId(1)));
            return target;
        }
    }
}
