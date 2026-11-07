using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Teams;
using Tanks.Shared.Tests.Entities;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Items.Behaviours
{
    public sealed class NoHonorTests : ItemBehaviourTestBase<NoHonorBehaviour>
    {
        private IEntityBehaviour _target;

        protected override void InitializeConfig() => AddItemStat("increase", new ItemStat { Value = 35 });

        public override void Setup()
        {
            base.Setup();

            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id, id });
            MockOwner.Entity.Transform.WorldPosition.Returns(new float2(5, 20));
            _target = Substitute.For<IEntityBehaviour>();
            _target.Transform.WorldPosition.Returns(new float2(10, 20));
            _target.Transform.Right.Returns(new float2(1, 0));
            _target.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(100, 100)));
            _target.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(1, "target", new TeamId(1)));
        }

        [TestCase(0, 170)]
        [TestCase(44, 170)]
        [TestCase(45, 170)]
        [TestCase(-45, 170)]
        [TestCase(46, 100)]
        [TestCase(-46, 100)]
        [TestCase(90, 100)]
        [TestCase(180, 100)]
        public void DealDamage_RearConeUsesAttackerPosition(float degrees, float expected)
        {
            float radians = math.radians(degrees);
            float2 direction = new float2(math.cos(radians), math.sin(radians));
            float2 position = _target.Transform.WorldPosition - direction * 5;
            MockOwner.Entity.Transform.WorldPosition.Returns(position);

            float damage = Hit(_target);

            Assert.That(damage, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void DealDamage_ReevaluatesPositionsAndTargetFacing()
        {
            _target.Transform.Right.Returns(new float2(0, 1));
            MockOwner.Entity.Transform.WorldPosition.Returns(new float2(10, 15));

            Assert.That(Hit(_target), Is.EqualTo(170).Within(0.0001f));

            MockOwner.Entity.Transform.WorldPosition.Returns(new float2(10, 25));

            Assert.That(Hit(_target), Is.EqualTo(100));

            _target.Transform.Right.Returns(new float2(0, -1));

            Assert.That(Hit(_target), Is.EqualTo(170).Within(0.0001f));
        }

        [Test]
        public void DealDamage_OverlappingPositionsDoNotReceiveBonus()
        {
            float2 position = _target.Transform.WorldPosition;
            MockOwner.Entity.Transform.WorldPosition.Returns(position);

            Assert.That(Hit(_target), Is.EqualTo(100));
        }

        [Test]
        public void DealDamage_SelfDamageDoesNotReceiveBonus()
        {
            Assert.That(Hit(MockOwner.Entity), Is.EqualTo(100));
        }

        [Test]
        public void DealDamage_NonPlayerDoesNotReceiveBonus()
        {
            _target.GetBehaviour<ITeamParticipantBehaviour>().Returns((ITeamParticipantBehaviour)null);

            Assert.That(Hit(_target), Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void DealDamage_DeadPlayerDoesNotReceiveBonus(int health)
        {
            _target.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(health, 100)));

            Assert.That(Hit(_target), Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void DealDamage_NonPositiveDamageDoesNotReceiveBonus(float amount)
        {
            Assert.That(Hit(_target, amount), Is.EqualTo(amount));
        }

        [TestCase(1, 135)]
        [TestCase(2, 170)]
        [TestCase(3, 205)]
        public void DealDamage_DamageServiceStacksCopiesAdditively(int copies, int expected)
        {
            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(Enumerable.Repeat(id, copies).ToArray());
            MockOwner.GetAllBehaviours().Returns(Enumerable.Repeat<ItemBehaviourBase>(Behaviour, copies));
            MockOwner.Entity.GetBehaviour<IItemOwnerBehaviour>().Returns(MockOwner);
            MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            _target.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            _target.IsValid.Returns(true);
            TestDamageService service = new();

            service.Damage(100, _target, MockOwner.Entity);

            Assert.That(service.FullAmount, Is.EqualTo(expected));

            service.Damage(100, _target, MockOwner.Entity, includeStats: false);

            Assert.That(service.FullAmount, Is.EqualTo(100));
        }

        private float Hit(IEntityBehaviour target, float damage = 100)
        {
            Behaviour.OnDealDamage(target, MockOwner, ref damage);
            return damage;
        }
    }
}
