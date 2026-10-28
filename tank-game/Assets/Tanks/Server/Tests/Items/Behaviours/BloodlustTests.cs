using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Items.Behaviours.HealthBased;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class BloodlustTests : ItemBehaviourTestBase<BloodlustBehaviour>
    {
        private IDamageService _damageService;

        protected override void InitializeConfig()
        {
            AddItemStat("interval", new ItemStat { Value = 10 });
            AddItemStat("damage", new ItemStat { Value = 15, ValueType = StatValueType.Percent });
        }

        protected override BloodlustBehaviour CreateBehaviour()
        {
            _damageService = Substitute.For<IDamageService>();
            return new BloodlustBehaviour(_damageService);
        }

        [Test]
        public void Pickup_Twice_Kills()
        {
            Behaviour.OnPickup(MockOwner);

            // Does not kill on first pickup
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);

            MockOwner.Items.Returns(new ItemIdentifier[]
            {
                new ItemIdentifier("bloodlust"),
                new ItemIdentifier("bloodlust")
            });

            Behaviour.OnPickup(MockOwner);

            // Kills on second pickup
            _damageService.Received(1)
                .Damage(uint.MaxValue, MockOwner.Entity, MockOwner.Entity, includeStats: false);
        }

        [Test]
        public void Pickup_AppliesImmediately()
        {
            MockOwner
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(current: 50, max: 100));

            Behaviour.OnPickup(MockOwner);

            IStatOwnerBehaviour statOwner = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            StatId targetStatId = Behaviour.StatId;

            ItemStat damage = Config.GetStat("damage");
            float fullDamage = 5 * damage.Value;

            statOwner.Received(1)
                .OverrideStat(targetStatId, StatType.Damage, damage.ValueType, fullDamage);
        }

        [Test]
        public void OnHealthChanged_AppliesImmediately()
        {
            MockOwner
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(current: 100, max: 100));

            Behaviour.OnPickup(MockOwner);

            IStatOwnerBehaviour statOwner = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            StatId targetStatId = Behaviour.StatId;

            float interval = Config.GetStat("interval").Value;
            ItemStat damage = Config.GetStat("damage");

            for (int current = 99; current >= 1; current--)
            {
                statOwner.ClearReceivedCalls();

                MockOwner
                    .GetBehaviour<IHealthBehaviour>().ServerState.Value
                    .Returns(new HealthState(current, max: 100));

                Behaviour.OnHealthChanged(
                    MockOwner, null,
                    new HealthState(current + 1, 100), new HealthState(current, 100));

                int count = (int)((100 - current) / interval);
                float fullBonus = count * damage.Value;

                statOwner.Received(1)
                    .OverrideStat(targetStatId, StatType.Damage, damage.ValueType, fullBonus);
            }
        }
    }
}
