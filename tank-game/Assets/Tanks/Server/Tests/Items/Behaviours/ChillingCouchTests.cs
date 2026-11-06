using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Adapters;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using Unity.Mathematics;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class ChillingCouchTests : ItemBehaviourTestBase<ChillingCouchBehaviour>
    {
        private IStatOwnerBehaviour _stats;
        private Dictionary<StatType, float> _values;

        protected override void InitializeConfig()
        {
            AddItemStat("interval", new ItemStat { Value = 10 });
            AddItemStat("increase", new ItemStat { Value = 1 });
        }

        public override void Setup()
        {
            base.Setup();
            MockOwner.IsServer.Returns(true);
            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id, id });
            MockOwner.Transform.WorldPosition.Returns(float2.zero);
            MockOwner.Transform.WorldRotation.Returns(Angle.FromDegrees(0));
            _stats = MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>();
            _values = new()
            {
                [StatType.Damage] = 30,
                [StatType.Speed] = 10,
                [StatType.Defense] = 20,
                [StatType.Resistance] = 5,
                [StatType.RateOfFire] = 3
            };

            _stats.GetStat(Arg.Any<StatType>(), Arg.Any<float>())
                .Returns(args => _values[(StatType)args[0]]);
            _stats.GetMaximum(Arg.Any<StatType>()).Returns(100);
            Behaviour.OnPickup(MockOwner);
        }

        [Test]
        public void Tick_WaitsFullIntervalAndStacksIncreaseOnce()
        {
            Behaviour.OnTick(MockOwner, 9, 9);
            _stats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);

            Behaviour.OnTick(MockOwner, 10, 1);

            _stats.Received(1).AddStat(StatType.Damage, StatValueType.Flat, 2);
        }

        [Test]
        public void Tick_SkipsHighestWhenCapped()
        {
            _stats.GetMaximum(StatType.Damage).Returns(30);
            Behaviour.OnTick(MockOwner, 10, 10);

            _stats.Received(1).AddStat(StatType.Defense, StatValueType.Flat, 2);
        }

        [Test]
        public void Tick_BodyRotationResetsProgress()
        {
            Behaviour.OnTick(MockOwner, 9, 9);
            MockOwner.Transform.WorldRotation.Returns(Angle.FromDegrees(1));
            Behaviour.OnTick(MockOwner, 10, 1);
            Behaviour.OnTick(MockOwner, 19, 9);

            _stats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);

            Behaviour.OnTick(MockOwner, 20, 1);

            _stats.Received(1).AddStat(StatType.Damage, StatValueType.Flat, 2);
        }

        [Test]
        public void Tick_AllStatsCapped_DoesNotAddAnything()
        {
            _stats.GetMaximum(Arg.Any<StatType>()).Returns(args => _values[(StatType)args[0]]);
            Behaviour.OnTick(MockOwner, 10, 10);

            _stats.DidNotReceiveWithAnyArgs().AddStat(default, default, default, default);
        }
    }
}
