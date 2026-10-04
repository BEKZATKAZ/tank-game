using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Items.Pickups;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Stats;

namespace Tanks.Server.Tests.Items
{
    public class PickupAreaTests
    {
        private IItemDatabase _database;
        private IItemOwnerBehaviour _owner;
        private IStatOwnerBehaviour _statOwner;
        private PickupArea _component;

        [SetUp]
        public void SetUp()
        {
            _database = Substitute.For<IItemDatabase>();
            _owner = Substitute.For<IItemOwnerBehaviour>();
            _statOwner = Substitute.For<IStatOwnerBehaviour>();

            _owner.GetBehaviour<IStatOwnerBehaviour>().Returns(_statOwner);
            _component = new PickupArea(_database);
            _component.Initialize(Substitute.For<IPickupBehaviour>());
        }

        [Test]
        public void PickUp_CallsPickupItem()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");
            PickupId pickupId = new PickupId(2);

            IItemConfig config = Substitute.For<IItemConfig>();
            config.PlayerStats.Returns(new Dictionary<StatType, PlayerStat>());
            config.ItemStats.Returns(new Dictionary<string, ItemStat>());

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, pickupId);

            _owner.Received(1).PickupItem(config, pickupId);
        }

        [Test]
        public void PickUp_WithPlayerStats_AddsStats()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");

            PlayerStat stat = new PlayerStat
            {
                ValueType = StatValueType.Flat,
                Value = 5,
                Duration = 10
            };

            IItemConfig config = Substitute.For<IItemConfig>();
            config.PlayerStats.Returns(new Dictionary<StatType, PlayerStat>
            {
                [StatType.Speed] = stat
            });

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, default);
            _statOwner.Received(1).AddStat(StatType.Speed, stat.ValueType, stat.Value, stat.Duration);
        }

        [Test]
        public void PickUp_NoPlayerStats_DoesNothing()
        {
            ItemIdentifier itemId = new ItemIdentifier("test-id");

            IItemConfig config = Substitute.For<IItemConfig>();
            config.PlayerStats.Returns(new Dictionary<StatType, PlayerStat>());

            _database.Get(itemId).Returns(config);
            _component.PickUp(_owner, itemId, default);

            _statOwner.DidNotReceiveWithAnyArgs()
                .AddStat(default, default, default, default);
        }
    }
}
