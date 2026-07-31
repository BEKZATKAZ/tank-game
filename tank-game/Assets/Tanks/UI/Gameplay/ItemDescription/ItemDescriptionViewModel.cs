using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using UnityEngine;

namespace Tanks.UI.Gameplay.ItemDescription
{
    public sealed class ItemDescriptionViewModel :
        IViewModel,
        IMessageHandler<PickupAreaEntered>,
        IMessageHandler<PickupAreaLeft>,
        IDisposable
    {
        public ItemDescriptionViewModel(
            ILocalizationDatabase localization,
            IClientItemDatabase clientItemDatabase,
            IItemDatabase itemDatabase,
            IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<PickupAreaEntered>(this);
            _bus.RegisterHandler<PickupAreaLeft>(this);

            _localization = localization;
            _localization.LocaleChanged += OnLocaleChanged;

            _clientDatabase = clientItemDatabase;
            _sharedDatabase = itemDatabase;
            _enteredPickups = new Dictionary<PickupId, ItemIdentifier>();
        }

        public BindableProperty<string> Name { get; } = new();
        public BindableProperty<string> Description { get; } = new();
        public BindableProperty<string> TierName { get; } = new();
        public BindableProperty<Sprite> Icon { get; } = new();
        public BindableProperty<Color> AccentColor { get; } = new();
        public BindableProperty<float> Opacity { get; } = new();

        private readonly Dictionary<PickupId, ItemIdentifier> _enteredPickups;
        private readonly ILocalizationDatabase _localization;
        private readonly IClientItemDatabase _clientDatabase;
        private readonly IItemDatabase _sharedDatabase;
        private readonly IMessageBus _bus;
        private ItemIdentifier _currentEnteredItemId;

        private void OnLocaleChanged(string locale)
        {
            if (_currentEnteredItemId == default) return;
            Reset(_currentEnteredItemId);
        }

        private void Reset(ItemIdentifier itemId)
        {
            IClientItemConfig clientConfig = _clientDatabase.Get(itemId);
            IItemConfig sharedConfig = _sharedDatabase.Get(itemId);

            string idString = itemId.ToString();
            string tierString = sharedConfig.Tier.ToString().ToLower();

            bool nameExists = _localization.TryGet(
                "Items", $"item.{idString}.name", out ILocalizedString name);

            bool descriptionExists = _localization.TryGet(
                "Items", $"item.{idString}.description", out ILocalizedString description);

            bool tierExists = _localization.TryGet(
                "Items", $"item.tier.{tierString}", out ILocalizedString tier);

            Name.Value = nameExists ? name.Get() : "";
            Description.Value = descriptionExists ? description.Get() : "";
            TierName.Value = tierExists ? tier.Get() : "";

            Icon.Value = clientConfig.Icon;
            AccentColor.Value = clientConfig.AccentColor;
            Opacity.Value = 1;
        }

        public void Handle(PickupAreaLeft message, IMessageSender sender)
        {
            _enteredPickups.Remove(message.PickupId);

            if (_enteredPickups.Count > 0)
            {
                _currentEnteredItemId = _enteredPickups.Last().Value;
                Reset(_currentEnteredItemId);
                return;
            }

            Opacity.Value = 0;
        }

        public void Handle(PickupAreaEntered message, IMessageSender sender)
        {
            _currentEnteredItemId = message.ItemId;
            _enteredPickups.Add(message.PickupId, message.ItemId);
            Reset(message.ItemId);
        }

        public void Dispose()
        {
            _localization.LocaleChanged -= OnLocaleChanged;
        }
    }
}
