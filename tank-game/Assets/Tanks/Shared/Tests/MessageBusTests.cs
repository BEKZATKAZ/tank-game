using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Messaging;

namespace Tanks.Shared.Tests
{
    public struct ItemAdded
    {
        public int Id;
        public int Amount;
    }

    public struct ItemRemoved
    {
        public int Id;
        public int Amount;
    }

    public class MessageBusTests
    {
        private IMessageBus _bus;
        private IMessageSender _sender;
        private IMessageHandler<ItemAdded> _addedHandler;
        private IMessageHandler<ItemRemoved> _removedHandler;

        [SetUp]
        public void Setup()
        {
            _addedHandler = Substitute.For<IMessageHandler<ItemAdded>>();
            _removedHandler = Substitute.For<IMessageHandler<ItemRemoved>>();

            _sender = Substitute.For<IMessageSender>();
            _bus = new MessageBus();

            _bus.RegisterHandler(_addedHandler.AsRequired());
            _bus.RegisterHandler(_removedHandler.AsRequired());
        }

        [Test]
        public void Unregister_DoesNotHandleAfter()
        {
            _bus.UnregisterHandler(_addedHandler.AsRequired());
            _bus.Publish(new ItemAdded(), _sender);
            _addedHandler.DidNotReceiveWithAnyArgs().Handle(Arg.Any<ItemAdded>(), _sender);
        }

        [Test]
        public void Unregister_NoHandlers_Throws()
        {
            _bus.UnregisterHandler(_addedHandler.AsRequired());

            Assert.Throws<ArgumentException>(() =>
                _bus.UnregisterHandler(_addedHandler.AsRequired()));
        }

        [Test]
        public void Register_DuplicateHandlers_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _bus.RegisterHandler(_addedHandler.AsRequired()));

            Assert.Throws<ArgumentException>(() =>
                _bus.RegisterHandler(_removedHandler.AsRequired()));
        }

        [Test]
        public void Publish_OneHandler_HandlesCorrectly()
        {
            ItemAdded added1 = new ItemAdded { Id = 1, Amount = 5 };
            ItemAdded added2 = new ItemAdded { Id = 2, Amount = 4 };

            _bus.Publish(new ItemAdded { Id = 1, Amount = 5 }, _sender);
            _addedHandler.Received(1).Handle(added1, _sender);

            _bus.Publish(added2, _sender);
            _addedHandler.Received(1).Handle(added2, _sender);
        }
    }
}
