using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnFire;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Utils;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class DiminishingReturnsTests : ItemBehaviourTestBase<DiminishingReturnsBehaviour>
    {
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
        }

        [Test]
        public void Hits_EverySecondHitReceivesStackedBonus()
        {
            DirectHitContext first = new() { DamageMultiplier = 1 };
            DirectHitContext second = new() { DamageMultiplier = 1 };
            DirectHitContext third = new() { DamageMultiplier = 1 };

            Behaviour.OnDirectHit(MockOwner, ref first);
            Behaviour.OnDirectHit(MockOwner, ref second);
            Behaviour.OnDirectHit(MockOwner, ref third);

            Assert.That(first.DamageMultiplier, Is.EqualTo(1));
            Assert.That(second.DamageMultiplier, Is.EqualTo(2));
            Assert.That(third.DamageMultiplier, Is.EqualTo(1));
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
        public void Hits_DoNotRequireOwnerServerFlag()
        {
            MockOwner.IsServer.Returns(false);
            DirectHitContext hit = new() { DamageMultiplier = 1 };
            Behaviour.OnDirectHit(MockOwner, ref hit);
            Behaviour.OnDirectHit(MockOwner, ref hit);

            Assert.That(hit.DamageMultiplier, Is.EqualTo(2));
        }
    }
}
