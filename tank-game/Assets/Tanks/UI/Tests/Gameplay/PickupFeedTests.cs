using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.UI.Gameplay.PickupFeed;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class PickupFeedTests : ViewModelTestBase<PickupFeedViewModel>
    {
        private IMessageBus _bus;
        private IClientItemDatabase _items;
        private ILocalizationDatabase _localization;
        private ILocalizedString _pickedUp;

        private readonly ItemIdentifier _itemId = new("test-item");

        protected override PickupFeedViewModel CreateViewModel()
        {
            _bus = Substitute.For<IMessageBus>();
            _items = Substitute.For<IClientItemDatabase>();
            _localization = Substitute.For<ILocalizationDatabase>();

            _items.Get(_itemId).Returns(Substitute.For<IClientItemConfig>());
            _items.Get(_itemId).AccentColor.Returns(Color.cyan);

            _pickedUp = Substitute.For<ILocalizedString>();
            _pickedUp.Get().Returns("picked up");
            _localization.TryGet("Items", "item.picked-up", out Arg.Any<ILocalizedString>())
                .Returns(x =>
                {
                    x[2] = _pickedUp;
                    return true;
                });

            return new PickupFeedViewModel(_bus, _items, _localization);
        }

        [Test]
        public void Constructor_RegistersHandler()
        {
            _bus.Received(1).RegisterHandler<ItemPickedUp>(ViewModel);
        }

        [Test]
        public void BusPublication_StopsAddingEntriesAfterDispose()
        {
            MessageBus bus = new MessageBus();
            PickupFeedViewModel model = new PickupFeedViewModel(bus, _items, _localization);
            ItemPickedUp message = CreatePickup("Player");

            bus.Publish(message, null);
            Assert.AreEqual(1, model.Entries.Value.Count);

            model.Dispose();

            bus.Publish(message, null);
            Assert.AreEqual(1, model.Entries.Value.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Handle_LocalOrRemotePlayer_SnapshotsDisplayNameAndItemAccent(bool possessed)
        {
            ItemPickedUp message = CreatePickup("Player", possessed);

            ViewModel.Handle(message, null);

            message.Owner.Entity.GetBehaviour<ITeamParticipantBehaviour>().State.Returns(
                new PlayerState(1, "Changed", default));

            Assert.AreEqual("Player", ViewModel.Entries.Value[0].Player);
            Assert.AreEqual("picked up", ViewModel.Entries.Value[0].PickedUp);
            Assert.AreEqual(Color.cyan, ViewModel.Entries.Value[0].AccentColor);
        }

        [Test]
        public void Handle_MissingParticipant_IgnoresPickup()
        {
            ViewModel.Handle(new ItemPickedUp(), null);

            IItemOwnerBehaviour owner = Substitute.For<IItemOwnerBehaviour>();
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();

            owner.Entity.Returns(entity);

            entity
                .GetBehaviour<ITeamParticipantBehaviour>()
                .Returns((ITeamParticipantBehaviour)null);

            ViewModel.Handle(new ItemPickedUp { Owner = owner, ItemId = _itemId }, null);

            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Handle_RepeatedItem_KeepsSeparateEntries()
        {
            ItemPickedUp message = CreatePickup("Player");

            ViewModel.Handle(message, null);
            PickupFeedEntry first = ViewModel.Entries.Value[0];

            ViewModel.Handle(message, null);

            Assert.AreEqual(2, ViewModel.Entries.Value.Count);
            Assert.AreSame(first, ViewModel.Entries.Value[1]);
            Assert.AreNotSame(first, ViewModel.Entries.Value[0]);
        }

        [Test]
        public void Handle_SixPickups_KeepsFiveNewest()
        {
            for (int i = 0; i < 6; i++) ViewModel.Handle(CreatePickup(i.ToString()), null);

            Assert.AreEqual(5, ViewModel.Entries.Value.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual((5 - i).ToString(), ViewModel.Entries.Value[i].Player);
            }
        }

        [Test]
        public void Tick_ExpiresEntriesIndependently()
        {
            ViewModel.Handle(CreatePickup("First"), null);
            ViewModel.Tick(2);

            ViewModel.Handle(CreatePickup("Second"), null);
            ViewModel.Tick(2.5f);

            Assert.AreEqual(2, ViewModel.Entries.Value.Count);
            ViewModel.Tick(0.5f);

            Assert.AreEqual(1, ViewModel.Entries.Value.Count);
            Assert.AreEqual("Second", ViewModel.Entries.Value[0].Player);

            ViewModel.Tick(2);
            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Tick_NonPositiveDelta_DoesNotAgeEntries()
        {
            ViewModel.Handle(CreatePickup("Player"), null);
            ViewModel.Tick(-5);
            ViewModel.Tick(0);
            ViewModel.Tick(4);

            Assert.AreEqual(1, ViewModel.Entries.Value.Count);

            ViewModel.Tick(1);

            Assert.IsEmpty(ViewModel.Entries.Value);
        }

        [Test]
        public void Element_ShowsPlainTextIconOnRightAndAccentOutline()
        {
            Texture2D texture = new Texture2D(1, 1);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);

            try
            {
                _items.Get(_itemId).Icon.Returns(icon);
                ViewModel.Handle(CreatePickup("<b>Player</b>"), null);

                PickupFeedEntry entry = ViewModel.Entries.Value[0];
                PickupFeedElement element = new PickupFeedElement();
                element.Set(entry);

                Label text = element.Q<Label>("Text");

                Assert.AreEqual("<b>Player</b> picked up", text.text);
                Assert.IsFalse(text.enableRichText);
                Assert.AreSame(element.Q("Icon"), element[1]);
                Assert.AreSame(icon, element[1].style.backgroundImage.value.sprite);

                Assert.AreEqual(Color.cyan, element.style.borderTopColor.value);
                Assert.AreEqual(Color.cyan, element.style.borderRightColor.value);
                Assert.AreEqual(Color.cyan, element.style.borderBottomColor.value);
                Assert.AreEqual(Color.cyan, element.style.borderLeftColor.value);
            }
            finally
            {
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(texture);
            }
        }

        [TestCase("подобрал(а)")]
        [TestCase("a ramassé")]
        public void Handle_UsesLocalizedPickedUpText(string translation)
        {
            _pickedUp.Get().Returns(translation);

            ViewModel.Handle(CreatePickup("Player"), null);

            Assert.AreEqual(translation, ViewModel.Entries.Value[0].PickedUp);
            _localization.DidNotReceive().TryGet(
                "Items", $"item.{_itemId}.name", out Arg.Any<ILocalizedString>());
        }

        private ItemPickedUp CreatePickup(string displayName, bool possessed = false)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            IItemOwnerBehaviour owner = Substitute.For<IItemOwnerBehaviour>();
            ITeamParticipantBehaviour participant = Substitute.For<ITeamParticipantBehaviour>();

            participant.State.Returns(new PlayerState(1, displayName, default));
            entity.GetBehaviour<ITeamParticipantBehaviour>().Returns(participant);

            entity.IsPossessed.Returns(possessed);
            owner.Entity.Returns(entity);

            return new ItemPickedUp { Owner = owner, ItemId = _itemId };
        }
    }
}