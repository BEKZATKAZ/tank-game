using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.ItemDescription;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class ItemDescriptionTests :
        PresenterTestBase<ItemDescriptionPresenter, ItemDescriptionModel>
    {
        private ILocalizationDatabase _localization;
        private IClientItemDatabase _clientDatabase;
        private IItemDatabase _sharedDatabase;
        private IMessageBus _bus;

        protected override ItemDescriptionPresenter CreatePresenter()
        {
            _localization = Substitute.For<ILocalizationDatabase>();
            _clientDatabase = Substitute.For<IClientItemDatabase>();
            _sharedDatabase = Substitute.For<IItemDatabase>();
            _bus = Substitute.For<IMessageBus>();

            return new ItemDescriptionPresenter(
                _localization, _clientDatabase, _sharedDatabase, _bus);
        }

        private void InitializeTryGet(string entryName, string value)
        {
            _localization
                .TryGet("Items", entryName, out Arg.Any<ILocalizedString>())
                .Returns(x =>
                {
                    if (string.IsNullOrEmpty(value)) return false;

                    ILocalizedString localized = Substitute.For<ILocalizedString>();
                    localized.Get().Returns(value);

                    // Set out parameter
                    x[2] = localized;
                    return true;
                });
        }

        private void RegisterMockItem(
            ItemIdentifier id, string name, string description, ItemTier tier, Color accentColor)
        {
            IItemConfig config = Substitute.For<IItemConfig>();
            IClientItemConfig clientConfig = Substitute.For<IClientItemConfig>();

            InitializeTryGet($"item.{id}.name", name);
            InitializeTryGet($"item.{id}.description", description);
            InitializeTryGet($"item.tier.{tier.ToString().ToLower()}", tier.ToString());

            config.Identifier.Returns(id);
            config.Tier.Returns(tier);

            clientConfig.Identifier.Returns(id);
            clientConfig.AccentColor.Returns(accentColor);

            _sharedDatabase.Get(id).Returns(config);
            _clientDatabase.Get(id).Returns(clientConfig);
        }

        [TestCase("Test Name", null)]
        [TestCase(null, "Test Description")]
        [TestCase(null, null)]
        [TestCase("Test Name", "Test Description")]
        public void Enter_ValidItem_ShowsCorrect(string name, string description)
        {
            ItemIdentifier id = new ItemIdentifier("test-id");
            ItemTier tier = ItemTier.Rare;
            Color accentColor = Color.red;

            RegisterMockItem(id, name, description, tier, accentColor);
            Presenter.Handle(new PickupAreaEntered { ItemId = id }, null);

            Assert.AreEqual(name ?? "", LatestShownModel.Name);
            Assert.AreEqual(description ?? "", LatestShownModel.Description);
            Assert.AreEqual(tier.ToString(), LatestShownModel.Tier);
            Assert.AreEqual(accentColor, LatestShownModel.AccentColor);

            _clientDatabase.Received(1).Get(Arg.Any<ItemIdentifier>());
            _sharedDatabase.Received(1).Get(Arg.Any<ItemIdentifier>());

            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("ru");

            // After changing locale, UI should refresh
            _clientDatabase.Received(2).Get(id);
            _sharedDatabase.Received(2).Get(id);
        }

        [Test]
        public void Exit_WithHistory_ShowsPreviousPickup()
        {
            ItemIdentifier id1 = new ItemIdentifier("id1");
            ItemIdentifier id2 = new ItemIdentifier("id2");

            RegisterMockItem(id1, "item-1", "", ItemTier.Epic, Color.red);
            RegisterMockItem(id2, "item-2", "", ItemTier.Legendary, Color.yellow);

            Presenter.Handle(new PickupAreaEntered { ItemId = id1, PickupId = new(1) }, null);
            Presenter.Handle(new PickupAreaEntered { ItemId = id2, PickupId = new(2) }, null);

            Assert.False(IsHidden, "Should be shown now");
            Assert.AreEqual("item-2", LatestShownModel.Name, "Should show the second item");

            Presenter.Handle(new PickupAreaLeft { PickupId = new PickupId(2) }, null);

            Assert.False(IsHidden, "Should be shown now");
            Assert.AreEqual("item-1", LatestShownModel.Name,
                "Should show the previous item from the history");

            Presenter.Handle(new PickupAreaLeft { PickupId = new PickupId(1) }, null);
            Assert.True(IsHidden, "No pick-ups left, the view should be hidden now");
        }

        [Test]
        public void Exit_NoHistory_HidesView()
        {
            Presenter.Handle(new PickupAreaLeft { PickupId = new PickupId(1) }, null);
            Assert.True(IsHidden, "No pick-ups were registered before, the view should be hidden");
        }
    }
}
