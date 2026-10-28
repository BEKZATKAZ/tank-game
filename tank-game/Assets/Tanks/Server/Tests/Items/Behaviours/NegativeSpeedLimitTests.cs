using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Entities;
using Tanks.Server.Items.Behaviours.DamageBased;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using Unity.Mathematics;
using SharedBehaviour = Tanks.Shared.Items.Behaviours.DamageBased.NegativeSpeedLimitBehaviour;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class NegativeSpeedLimitTests : ItemBehaviourTestBase<NegativeSpeedLimitBehaviour>
    {
        private IDamageService _damageService;

        protected override void InitializeConfig()
        {
            AddItemStat("damage", new ItemStat { Value = 8 });
            AddItemStat("decrease", new ItemStat { Value = 90 });
        }

        protected override NegativeSpeedLimitBehaviour CreateBehaviour()
        {
            _damageService = Substitute.For<IDamageService>();
            return new NegativeSpeedLimitBehaviour(_damageService);
        }

        public override void Setup()
        {
            base.Setup();
            MockOwner.Transform.Right.Returns(new float2(1, 0));
        }

        [TestCase(1, 0, true)]
        [TestCase(0.8f, 0.6f, true)]
        [TestCase(0.6f, 0.8f, false)]
        [TestCase(0.01f, 0, false)]
        [TestCase(-1, 0, false)]
        [TestCase(0, 1, false)]
        [TestCase(0, 0, false)]
        [TestCase(11, 0, false)]
        public void Tick_OnlyForwardMovementCausesDamage(float x, float y, bool shouldDamage)
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(x, y));

            Behaviour.OnTick(MockOwner, 10, 0.02f);

            if (shouldDamage) AssertDamage(MockOwner, 10);
            else _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [Test]
        public void Tick_UsesCurrentFacingDirection()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.Right.Returns(new float2(0, -1));
            MockOwner.Transform.WorldPosition.Returns(new float2(0, -1));

            Behaviour.OnTick(MockOwner, 10, 0.02f);

            AssertDamage(MockOwner, 10);
        }

        [Test]
        public void Tick_ForwardMovementBetweenIntervals_DamagesOnceThenResets()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));

            Behaviour.OnTick(MockOwner, 1, 0.02f);
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);

            Behaviour.OnTick(MockOwner, 10, 0.02f);
            AssertDamage(MockOwner, 10);
            _damageService.ClearReceivedCalls();

            Behaviour.OnTick(MockOwner, 20, 0.02f);

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [Test]
        public void Tick_ReversingAfterForwardMovement_DoesNotReusePickupPosition()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(2, 0));
            Behaviour.OnTick(MockOwner, 10, 0.02f);
            _damageService.ClearReceivedCalls();

            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));
            Behaviour.OnTick(MockOwner, 20, 0.02f);

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [Test]
        public void Tick_AfterTeleport_TracksMovementFromNewPosition()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(100, 0));
            Behaviour.OnTick(MockOwner, 10, 0.02f);
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);

            MockOwner.Transform.WorldPosition.Returns(new float2(101, 0));
            Behaviour.OnTick(MockOwner, 20, 0.02f);

            AssertDamage(MockOwner, 20);
        }

        [Test]
        public void Pickup_AtNonzeroPosition_DoesNotCountSpawnAsMovement()
        {
            MockOwner.Transform.WorldPosition.Returns(new float2(5, 0));
            Behaviour.OnPickup(MockOwner);

            Behaviour.OnTick(MockOwner, 10, 0.02f);

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [Test]
        public void Pickup_Twice_PreservesPendingDamage()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));
            Behaviour.OnTick(MockOwner, 1, 0.02f);

            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 10, 0.02f);

            AssertDamage(MockOwner, 10);
        }

        [Test]
        public void Destroy_ClearsPendingDamageAndPickupStartsAtCurrentPosition()
        {
            Behaviour.OnPickup(MockOwner);
            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));
            Behaviour.OnTick(MockOwner, 1, 0.02f);
            Behaviour.OnDestroy(MockOwner);
            Behaviour.OnTick(MockOwner, 10, 0.02f);

            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 20, 0.02f);

            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [Test]
        public void Tick_OwnersHaveIndependentMovementAndPendingDamage()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            other.Transform.Right.Returns(new float2(1, 0));
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(other);
            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));

            Behaviour.OnTick(MockOwner, 1, 0.02f);
            Behaviour.OnTick(other, 10, 0.02f);
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);

            Behaviour.OnTick(MockOwner, 10, 0.02f);

            AssertDamage(MockOwner, 10);
        }

        [Test]
        public void Tick_AuthoritativeDamageService_AppliesSharedSelfDamageReduction()
        {
            SharedBehaviour reduction = new SharedBehaviour();
            reduction.Initialize(Config);

            MockOwner.Entity.IsValid.Returns(true);
            MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>().Returns((IStatOwnerBehaviour)null);
            MockOwner.Entity.GetBehaviour<IItemOwnerBehaviour>().Returns(MockOwner);
            MockOwner.GetAllBehaviours().Returns(new ItemBehaviourBase[] { reduction });

            IAuthoritativeHealth health = MockOwner.Entity
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            NegativeSpeedLimitBehaviour behaviour = new NegativeSpeedLimitBehaviour(new AuthoritativeDamageService());
            behaviour.Initialize(Config);
            behaviour.OnPickup(MockOwner);

            MockOwner.Transform.WorldPosition.Returns(new float2(1, 0));
            behaviour.OnTick(MockOwner, 10, 0.02f);

            HealthEventId eventId = HealthEventId.Create(typeof(NegativeSpeedLimitBehaviour), 10);
            health.Received(1).Damage(8, MockOwner.Entity, eventId);
        }

        private void AssertDamage(IItemOwnerBehaviour owner, int tick)
        {
            _damageService.Received(1).Damage(
                8, owner.Entity, owner.Entity,
                HealthEventId.Create(typeof(NegativeSpeedLimitBehaviour), tick), false);
        }
    }
}
