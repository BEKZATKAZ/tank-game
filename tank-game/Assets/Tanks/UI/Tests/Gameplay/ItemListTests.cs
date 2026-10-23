using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Client.Items;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.ItemList;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public class ItemListTests
    {
        private MessageBus _bus;
        private IClientItemDatabase _database;
        private IClientEntityService _entities;
        private ItemListViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _bus = new MessageBus();
            _database = Substitute.For<IClientItemDatabase>();
            _entities = Substitute.For<IClientEntityService>();
            _database.Get(Arg.Any<ItemIdentifier>()).Returns(Substitute.For<IClientItemConfig>());
        }

        [TearDown]
        public void TearDown() => _viewModel?.Dispose();

        [Test]
        public void Constructor_GroupsExistingInventoryInFirstPickupOrder()
        {
            IItemOwnerBehaviour owner = CreateOwner("a", "b", "a");
            SetPossessed(owner.Entity);
            Assert.AreSame(owner.Entity, _entities.GetPossessedEntity());
            Assert.IsTrue(owner.Entity.IsValid);
            Assert.AreEqual(3, owner.Items.Count());
            _viewModel = CreateViewModel();

            CollectionAssert.AreEqual(new[] { "a", "b" },
                _viewModel.Entries.Value.Select(x => x.Id.ToString()));
            CollectionAssert.AreEqual(new[] { 2, 1 }, _viewModel.Entries.Value.Select(x => x.Count));
        }

        [Test]
        public void Pickup_StacksToThreeWithoutReordering()
        {
            IItemOwnerBehaviour owner = CreateOwner("a", "b");
            SetPossessed(owner.Entity);
            _viewModel = CreateViewModel();

            Pickup(owner, "a");
            Pickup(owner, "a");
            Pickup(owner, "c");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" },
                _viewModel.Entries.Value.Select(x => x.Id.ToString()));
            CollectionAssert.AreEqual(new[] { 3, 1, 1 }, _viewModel.Entries.Value.Select(x => x.Count));
        }

        [Test]
        public void Pickup_StacksDuplicatesAndRetrievesIcon()
        {
            IItemOwnerBehaviour owner = CreateOwner();
            SetPossessed(owner.Entity);
            Texture2D texture = new Texture2D(2, 2);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            _database.Get(new ItemIdentifier("a")).Icon.Returns(icon);
            _viewModel = CreateViewModel();

            try
            {
                Pickup(owner, "a");
                Pickup(owner, "b");
                Pickup(owner, "a");
                Pickup(owner, "a");

                Assert.AreEqual(2, _viewModel.Entries.Value.Count);
                Assert.AreEqual(3, _viewModel.Entries.Value[0].Count);
                Assert.AreSame(icon, _viewModel.Entries.Value[0].Icon);
            }
            finally
            {
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Pickup_IgnoresUnpossessedAndOtherOwners()
        {
            IItemOwnerBehaviour owner = CreateOwner();
            SetPossessed(owner.Entity);
            _viewModel = CreateViewModel();
            owner.Entity.IsPossessed.Returns(false);

            Pickup(owner, "a");
            Pickup(CreateOwner(), "b");

            Assert.IsEmpty(_viewModel.Entries.Value);
        }

        [Test]
        public void PossessionChange_ReplacesInventoryAndClearsUnavailableOwner()
        {
            IItemOwnerBehaviour previous = CreateOwner("a", "a");
            IItemOwnerBehaviour current = CreateOwner("b");
            SetPossessed(previous.Entity);
            _viewModel = CreateViewModel();

            SetPossessed(current.Entity);
            _entities.Changed += Raise.Event<PossessedEntityChanged>(previous.Entity, current.Entity);
            Assert.AreEqual("b", _viewModel.Entries.Value.Single().Id.ToString());

            IEntityBehaviour noOwner = Substitute.For<IEntityBehaviour>();
            noOwner.IsValid.Returns(true);
            noOwner.GetBehaviour<IItemOwnerBehaviour>().Returns(null as IItemOwnerBehaviour);
            _entities.Changed += Raise.Event<PossessedEntityChanged>(current.Entity, noOwner);
            Assert.IsEmpty(_viewModel.Entries.Value);

            _entities.Changed += Raise.Event<PossessedEntityChanged>(noOwner, current.Entity);
            _entities.Changed += Raise.Event<PossessedEntityChanged>(current.Entity, null);
            Assert.IsEmpty(_viewModel.Entries.Value);
        }

        [Test]
        public void Constructor_NoPossessedEntity_HasEmptyInventory()
        {
            _entities.GetPossessedEntity().Returns(null as IEntityBehaviour);
            _viewModel = CreateViewModel();
            Assert.IsEmpty(_viewModel.Entries.Value);
        }

        [Test]
        public void Dispose_UnsubscribesFromPickupAndPossession()
        {
            IItemOwnerBehaviour owner = CreateOwner();
            SetPossessed(owner.Entity);
            _viewModel = CreateViewModel();
            ItemListViewModel disposed = _viewModel;
            disposed.Dispose();
            _viewModel = null;

            Pickup(owner, "a");
            _entities.Changed += Raise.Event<PossessedEntityChanged>(owner.Entity, CreateOwner("b").Entity);
            Assert.IsEmpty(disposed.Entries.Value);
        }

        private void SetPossessed(IEntityBehaviour entity)
        {
            _entities.GetPossessedEntity().Returns(entity);
        }

        private ItemListViewModel CreateViewModel() => new(_bus, _database, _entities);

        private void Pickup(IItemOwnerBehaviour owner, string id)
        {
            _bus.Publish(new ItemPickedUp { Owner = owner, ItemId = new ItemIdentifier(id) }, null);
        }

        private static IItemOwnerBehaviour CreateOwner(params string[] items)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            IItemOwnerBehaviour owner = Substitute.For<IItemOwnerBehaviour>();
            entity.IsValid.Returns(true);
            entity.IsPossessed.Returns(true);
            entity.GetBehaviour<IItemOwnerBehaviour>().Returns(owner);
            owner.Entity.Returns(entity);
            owner.Items.Returns(items.Select(x => new ItemIdentifier(x)).ToArray());
            return owner;
        }
    }
}
