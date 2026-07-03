using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Messaging;

namespace Tanks.Shared.Tests
{
    struct ItemAdded
    {
        public int Id;
        public int Amount;
    }

    struct ItemRemoved
    {
        public int Id;
        public int Amount;
    }

    class ItemAddedHandler : IMessageHandler<ItemAdded>
    {
        public ItemAddedHandler(Inventory inventory) => _inventory = inventory;

        private readonly Inventory _inventory;

        public void Handle(ItemAdded message, IMessageSender sender)
        {
            if (_inventory.Items.ContainsKey(message.Id) == false)
            {
                _inventory.Items.Add(message.Id, 0);
            }

            _inventory.Items[message.Id] += message.Amount;
            _inventory.Contributors.Add(sender);
        }
    }

    class ItemRemovedHandler : IMessageHandler<ItemRemoved>
    {
        public ItemRemovedHandler(Inventory inventory) => _inventory = inventory;

        private readonly Inventory _inventory;

        public void Handle(ItemRemoved message, IMessageSender sender)
        {
            _inventory.Items[message.Id] -= message.Amount;
            _inventory.Contributors.Add(sender);
        }
    }

    class InventoryMessageSender : IMessageSender
    {
        
    }

    class Inventory
    {
        public Dictionary<int, int> Items { get; } = new();
        public HashSet<IMessageSender> Contributors { get; } = new();
    }

    public class MessageBusTests
    {
        private IMessageBus _bus;
        private Inventory _inventory;
        private InventoryMessageSender _sender;

        [SetUp]
        public void Setup()
        {
            _inventory = new Inventory();
            _sender = new InventoryMessageSender();
            _bus = new MessageBus();

            _bus.RegisterHandler(new ItemAddedHandler(_inventory));
            _bus.RegisterHandler(new ItemRemovedHandler(_inventory));
        }

        [Test]
        public void Register_DuplicateHandlers_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _bus.RegisterHandler(new ItemAddedHandler(_inventory)));

            Assert.Throws<ArgumentException>(() =>
                _bus.RegisterHandler(new ItemRemovedHandler(_inventory)));
        }

        [Test]
        public void Publish_OneHandler_HandlesCorrectly()
        {
            _bus.Publish(new ItemAdded { Id = 1, Amount = 5 }, _sender);

            Assert.AreEqual(1, _inventory.Items.Count);
            Assert.AreEqual(5, _inventory.Items[1]);

            _bus.Publish(new ItemAdded { Id = 2, Amount = 4 }, _sender);

            Assert.AreEqual(2, _inventory.Items.Count);
            Assert.AreEqual(4, _inventory.Items[2]);

            Assert.AreEqual(1, _inventory.Contributors.Count);
            Assert.AreEqual(_sender, _inventory.Contributors.First());
        }
    }
}
