using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class HibernationTests : ItemBehaviourTestBase<HibernationBehaviour>
    {
        private IDamageService _damage;
        private EffectOwnerBehaviour _effects;

        protected override void InitializeConfig() => AddItemStat("duration", new ItemStat { Value = 5 });

        protected override HibernationBehaviour CreateBehaviour()
        {
            _damage = Substitute.For<IDamageService>();
            return new HibernationBehaviour(_damage);
        }

        public override void Setup()
        {
            base.Setup();
            _effects = new GameObject().AddComponent<EffectOwnerBehaviour>();
            MockOwner.Entity.GetBehaviour<IEffectOwnerBehaviour>().Returns(_effects);
            MockOwner.Entity.GetBehaviour<IHealthBehaviour>().ServerState
                .Returns(new BindableProperty<HealthState>(new HealthState(10, 100)));
        }

        [TearDown]
        public void TearDown()
        {
            Behaviour.OnDestroy(MockOwner);
            Object.DestroyImmediate(_effects.gameObject);
        }

        [Test]
        public void NaturalExpiry_FullyHealsOnce()
        {
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(4, 1);
            _damage.DidNotReceiveWithAnyArgs().Heal(default, default);

            _effects.OnTick(5, 1);
            _effects.OnTick(6, 1);

            _damage.Received(1).Heal(100, MockOwner.Entity);
        }

        [Test]
        public void BurningThawsEarly_CancelsHealingPermanently()
        {
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(2, 1);
            _effects.AddEffect(EffectType.Burning, 1);
            _effects.AddEffect(EffectType.Frozen, 1);
            _effects.OnTick(10, 1);

            _damage.DidNotReceiveWithAnyArgs().Heal(default, default);
        }

        [Test]
        public void RepeatPickup_RestartsFreezeAndHealingTimer()
        {
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(3, 1);
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(5, 1);

            _damage.DidNotReceiveWithAnyArgs().Heal(default, default);

            _effects.OnTick(8, 1);

            _damage.Received(1).Heal(100, MockOwner.Entity);
        }

        [Test]
        public void Pickup_DoesNotRequireOwnerServerFlag()
        {
            MockOwner.IsServer.Returns(false);
            Behaviour.OnPickup(MockOwner);

            Assert.That(_effects.Effects.ContainsKey(EffectType.Frozen), Is.True);
        }

        [Test]
        public void OtherFrozenId_RemainsIndependentAndDelaysHealingUntilThaw()
        {
            EffectId other = _effects.AddEffect(EffectType.Frozen, 10);
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(5, 1);

            Assert.That(_effects.HasEffect(other), Is.True);
            Assert.That(_effects.Entries[other].Duration, Is.EqualTo(10));
            _damage.DidNotReceiveWithAnyArgs().Heal(default, default);

            _effects.OnTick(10, 1);

            _damage.Received(1).Heal(100, MockOwner.Entity);
        }

        [Test]
        public void RemovingHibernationId_CancelsHealingEvenIfAnotherFreezeRemains()
        {
            EffectId other = _effects.AddEffect(EffectType.Frozen, 10);
            Behaviour.OnPickup(MockOwner);
            EffectId hibernation = default;

            foreach (EffectId id in _effects.Entries.Keys)
            {
                if (id.Equals(other) == false) hibernation = id;
            }

            _effects.RemoveEffect(hibernation);
            _effects.OnTick(10, 1);

            _damage.DidNotReceiveWithAnyArgs().Heal(default, default);
        }
    }
}
