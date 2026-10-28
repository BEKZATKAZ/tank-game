using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class StatsLightTests : ItemBehaviourTestBase<StatsLightBehaviour>
    {
        private readonly StatType[] _cycle =
        {
            StatType.Speed, StatType.Damage, StatType.Defense,
            StatType.RateOfFire, StatType.Resistance
        };

        protected override void InitializeConfig()
        {
            AddItemStat("interval", new ItemStat { Value = 9 });
            AddItemStat("speed-boost", new ItemStat { Value = 25, ValueType = StatValueType.Percent });
            AddItemStat("damage-boost", new ItemStat { Value = 50, ValueType = StatValueType.Percent });
            AddItemStat("defense-boost", new ItemStat { Value = 15 });
            AddItemStat("rate-of-fire-boost", new ItemStat { Value = 40, ValueType = StatValueType.Percent });
            AddItemStat("resistance-boost", new ItemStat { Value = 5 });
        }

        [TestCase(0.001f)]
        [TestCase(0.02f)]
        public void Tick_FirstTick_StartsCycle(float tickDelta)
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, tickDelta);

            MockOwner.GetBehaviour<IStatOwnerBehaviour>().Received(1)
                .AddStat(StatType.Speed, StatValueType.Percent, 25, 9);
        }

        [Test]
        public void Tick_CyclesInOrderAndRepeats()
        {
            string[] names = { "speed-boost", "damage-boost", "defense-boost", "rate-of-fire-boost", "resistance-boost" };
            IStatOwnerBehaviour stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            Behaviour.OnPickup(MockOwner);

            for (int i = 0; i < 11; i++)
            {
                stats.ClearReceivedCalls();
                Behaviour.OnTick(MockOwner, i * 9, 1);
                ItemStat boost = Config.GetStat(names[i % names.Length]);
                stats.Received(1).AddStat(_cycle[i % _cycle.Length], boost.ValueType, boost.Value, 9);
            }
        }

        [Test]
        public void Tick_BeforeInterval_DoesNotAdvance()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 0.5f);
            IStatOwnerBehaviour stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            stats.ClearReceivedCalls();

            Behaviour.OnTick(MockOwner, 17, 0.5f);
            stats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);
            Behaviour.OnTick(MockOwner, 18, 0.5f);
            stats.Received(1).AddStat(StatType.Damage, StatValueType.Percent, 50, 9);
        }

        [Test]
        public void Tick_RemovesPreviousBoostBeforeAddingNext()
        {
            IStatOwnerBehaviour stats = MockOwner.GetBehaviour<IStatOwnerBehaviour>();
            StatId id = StatIdGenerator.GenerateNext();
            stats.AddStat(Arg.Any<StatType>(), Arg.Any<StatValueType>(), Arg.Any<float>(), Arg.Any<float>()).Returns(id);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 1);
            stats.ClearReceivedCalls();

            Behaviour.OnTick(MockOwner, 9, 1);

            Received.InOrder(() =>
            {
                stats.RemoveStat(id);
                stats.AddStat(StatType.Damage, StatValueType.Percent, 50, 9);
            });
        }

        [Test]
        public void Pickup_Twice_DoesNotLeaveAnExtraCycleAfterDestroy()
        {
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnDestroy(MockOwner);
            Behaviour.OnTick(MockOwner, 0, 1);

            MockOwner.GetBehaviour<IStatOwnerBehaviour>().DidNotReceiveWithAnyArgs()
                .AddStat(default, default, default, default);
        }

        [Test]
        public void Tick_OwnersHaveIndependentCycles()
        {
            IItemOwnerBehaviour other = Substitute.For<IItemOwnerBehaviour>();
            other.Entity.Id.Returns(new EntityId(2));
            Behaviour.OnPickup(MockOwner);
            Behaviour.OnPickup(other);
            Behaviour.OnTick(MockOwner, 0, 1);
            Behaviour.OnTick(MockOwner, 9, 1);
            Behaviour.OnTick(other, 9, 1);

            other.GetBehaviour<IStatOwnerBehaviour>().Received(1)
                .AddStat(StatType.Speed, StatValueType.Percent, 25, 9);
        }
    }
}
