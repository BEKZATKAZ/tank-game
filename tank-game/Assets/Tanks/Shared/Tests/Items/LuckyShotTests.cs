using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Tests.Items
{
    public class LuckyShotTests : ItemBehaviourTestBase<LuckyShotBehaviour>
    {
        protected override void InitializeConfig()
        {
            AddItemStat("damage", new ItemStat { Value = 50 });
        }

        [Test]
        public void DealDamage_EveryThirdHit_IncreasesDamageAndRepeats()
        {
            Behaviour.OnPickup(MockOwner);

            for (int hit = 1; hit <= 9; hit++)
            {
                Assert.That(Hit(MockOwner), Is.EqualTo(hit % 3 == 0 ? 30 : 20), $"Hit {hit}");
            }
        }

        [Test]
        public void Fire_WithoutHit_DoesNotAdvanceCounter()
        {
            Behaviour.OnPickup(MockOwner);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));
            Behaviour.OnFire(MockOwner, default);
            Behaviour.OnFire(MockOwner, default);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));
            Assert.That(Hit(MockOwner), Is.EqualTo(30));
        }

        [Test]
        public void DealDamage_OwnersHaveIndependentCounters()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(other);

            Hit(MockOwner);
            Hit(MockOwner);
            Assert.That(Hit(other), Is.EqualTo(20));
            Assert.That(Hit(MockOwner), Is.EqualTo(30));
            Assert.That(Hit(other), Is.EqualTo(20));
            Assert.That(Hit(other), Is.EqualTo(30));
        }

        [Test]
        public void Pickup_Twice_PreservesHitCounter()
        {
            Behaviour.OnPickup(MockOwner);
            Hit(MockOwner);
            Behaviour.OnPickup(MockOwner);

            Assert.That(Hit(MockOwner, 2), Is.EqualTo(20));
            Assert.That(Hit(MockOwner, 2), Is.EqualTo(30));
        }

        [TestCase(2, 45)]
        [TestCase(3, 67.5f)]
        public void DealDamage_MultipleCopies_StackEveryThirdHit(int copies, float expected)
        {
            for (int i = 0; i < copies; i++) Behaviour.OnPickup(MockOwner);

            for (int hit = 1; hit <= 9; hit++)
            {
                Assert.That(Hit(MockOwner, copies), Is.EqualTo(hit % 3 == 0 ? expected : 20), $"Hit {hit}");
            }
        }

        [Test]
        public void Pickup_AdditionalCopy_StartsItsOwnCounter()
        {
            Behaviour.OnPickup(MockOwner);
            Hit(MockOwner);
            Hit(MockOwner);
            Behaviour.OnPickup(MockOwner);

            Assert.That(Hit(MockOwner, 2), Is.EqualTo(30));
            Assert.That(Hit(MockOwner, 2), Is.EqualTo(20));
            Assert.That(Hit(MockOwner, 2), Is.EqualTo(30));
            Assert.That(Hit(MockOwner, 2), Is.EqualTo(30));
        }

        [Test]
        public void Destroy_OneCopy_RemainingCopyKeepsItsCounter()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);
            Hit(MockOwner, 2);
            Behaviour.OnDestroy(MockOwner);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));
            Assert.That(Hit(MockOwner), Is.EqualTo(30));

            Behaviour.OnDestroy(MockOwner);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));
        }

        [Test]
        public void DealDamage_DamageService_StacksCopiesOnThirdHit()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);
            MockOwner.GetAllBehaviours().Returns(new ItemBehaviourBase[] { Behaviour, Behaviour });
            MockOwner.Entity.IsValid.Returns(true);
            MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            MockOwner.Entity.GetBehaviour<IItemOwnerBehaviour>().Returns(MockOwner);

            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            target.IsValid.Returns(true);
            target.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            TestDamageService service = new TestDamageService();

            for (int hit = 1; hit <= 6; hit++)
            {
                service.Damage(20, target, MockOwner.Entity);

                Assert.That(service.LastDamage, Is.EqualTo(hit % 3 == 0 ? 45 : 20), $"Hit {hit}");
            }
        }

        [Test]
        public void Destroy_RemovesCounterAndPickupStartsOver()
        {
            Behaviour.OnPickup(MockOwner);
            Hit(MockOwner);
            Hit(MockOwner);
            Behaviour.OnDestroy(MockOwner);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));

            Behaviour.OnPickup(MockOwner);

            Assert.That(Hit(MockOwner), Is.EqualTo(20));
            Assert.That(Hit(MockOwner), Is.EqualTo(20));
            Assert.That(Hit(MockOwner), Is.EqualTo(30));
        }

        [Test]
        public void Destroy_WithoutPickup_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Behaviour.OnDestroy(MockOwner));
        }

        private float Hit(IItemOwnerBehaviour owner, int copies = 1)
        {
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            float damage = 20;
            for (int i = 0; i < copies; i++) Behaviour.OnDealDamage(target, owner, ref damage);
            return damage;
        }

        private sealed class TestDamageService : DamageServiceBase
        {
            public uint LastDamage { get; private set; }

            protected override void OnDamage(
                uint fullAmount, IEntityBehaviour target,
                IEntityBehaviour from = null, HealthEventId eventId = default)
            {
                LastDamage = fullAmount;
            }

            protected override void OnHeal(uint amount, IEntityBehaviour target) { }
        }
    }
}
