using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Server.Items.Pickups;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Stats;

namespace Tanks.Server.Tests.Items
{
    public class PickupAreaTests
    {
        private IItemDatabase _database;
        private IItemOwner _owner;
        private IHealable _healable;
        private IStatOwner _statOwner;
        private PickupArea _component;

        [SetUp]
        public void SetUp()
        {
            _database = Substitute.For<IItemDatabase>();
            _owner = Substitute.For<IItemOwner>();
            _healable = Substitute.For<IHealable>();
            _statOwner = Substitute.For<IStatOwner>();

            _owner.Healable.Returns(_healable);
            _owner.StatOwner.Returns(_statOwner);

            _component = new PickupArea(_database);
            _component.Initialize(Substitute.For<IPickupBehaviour>());
        }

        [Test]
        public void PickUp_CallsPickupItem()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");
            PickupId pickupId = new PickupId(2);

            IItemConfig config = Substitute.For<IItemConfig>();
            config.Stats.Returns(Array.Empty<ItemStat>());

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, pickupId);

            _owner.Received(1).PickupItem(config, pickupId);
        }

        [Test]
        public void PickUp_WithStats_AddsStats()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");

            ItemStat stat = new ItemStat
            {
                ApplyOnPickUp = true,
                Type = StatType.Speed,
                ValueType = StatValueType.Flat,
                Value = 5,
                Duration = 10
            };

            IItemConfig config = Substitute.For<IItemConfig>();
            config.Stats.Returns(new[] { stat });

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, default);

            _statOwner.Received(1)
                .AddStat(stat.Type, stat.ValueType, stat.Value, stat.Duration);

            _healable.DidNotReceiveWithAnyArgs().Heal(default);
        }

        [Test]
        public void PickUp_NoApplyOnPickUp_DoesNothing()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");
            IItemConfig config = Substitute.For<IItemConfig>();

            config.Stats.Returns(new[]
            {
                new ItemStat
                {
                    ApplyOnPickUp = false,
                    Type = StatType.Speed,
                    Value = 100
                }
            });

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, default);

            _statOwner.DidNotReceiveWithAnyArgs()
                .AddStat(default, default, default, default);
        }
    }
}
