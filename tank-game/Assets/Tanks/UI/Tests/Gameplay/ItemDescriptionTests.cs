using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Localization;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.ItemDescription;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class ItemDescriptionTests : ViewModelTestBase<ItemDescriptionViewModel>
    {
        private ILocalizationDatabase _localization;
        private IClientItemDatabase _clientDatabase;
        private IItemDatabase _sharedDatabase;
        private IMessageBus _bus;

        protected override ItemDescriptionViewModel CreateViewModel()
        {
            _localization = Substitute.For<ILocalizationDatabase>();
            _clientDatabase = Substitute.For<IClientItemDatabase>();
            _sharedDatabase = Substitute.For<IItemDatabase>();
            _bus = Substitute.For<IMessageBus>();

            return new ItemDescriptionViewModel(
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
            ViewModel.Handle(new PickupAreaEntered { ItemId = id }, null);

            Assert.AreEqual(name ?? "", ViewModel.Name.Value);
            Assert.AreEqual(description ?? "", ViewModel.Description.Value);
            Assert.AreEqual(tier.ToString(), ViewModel.TierName.Value);
            Assert.AreEqual(accentColor, ViewModel.AccentColor.Value);

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

            ViewModel.Handle(new PickupAreaEntered { ItemId = id1, PickupId = new(1) }, null);
            ViewModel.Handle(new PickupAreaEntered { ItemId = id2, PickupId = new(2) }, null);

            Assert.AreEqual(1, ViewModel.Opacity.Value);
            Assert.AreEqual("item-2", ViewModel.Name.Value,
                "Should show the second item");

            ViewModel.Handle(new PickupAreaLeft { PickupId = new PickupId(2) }, null);

            Assert.AreEqual(1, ViewModel.Opacity.Value, "Should be shown now");
            Assert.AreEqual("item-1", ViewModel.Name.Value,
                "Should show the previous item from the history");

            ViewModel.Handle(new PickupAreaLeft { PickupId = new PickupId(1) }, null);
            Assert.AreEqual(0, ViewModel.Opacity.Value,
                "No pick-ups left, the view should be hidden now");
        }

        [Test]
        public void Exit_NoHistory_HidesView()
        {
            ViewModel.Handle(new PickupAreaLeft { PickupId = new PickupId(1) }, null);

            Assert.AreEqual(0, ViewModel.Opacity.Value,
                "No pick-ups were registered before, the view should be hidden");
        }

        [Test]
        public void ResetRound_HidesImmediately()
        {
            ItemIdentifier id = new ItemIdentifier("id1");

            ViewModel.Handle(new PickupAreaEntered { ItemId = id, PickupId = new(1) }, null);
            Assert.AreEqual(ViewModel.Opacity.Value, 1);

            ViewModel.Handle(new TilemapGenerated(), null);
            Assert.AreEqual(ViewModel.Opacity.Value, 0);
        }
    }
}
