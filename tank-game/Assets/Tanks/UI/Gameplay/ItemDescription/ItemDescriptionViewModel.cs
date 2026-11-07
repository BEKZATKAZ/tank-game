using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Effects;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Localization;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.UI.Items;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Gameplay.ItemDescription
{
    public sealed class ItemDescriptionViewModel :
        IViewModel,
        IMessageHandler<TilemapGenerated>,
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
            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<PickupAreaEntered>(this);
            _bus.RegisterHandler<PickupAreaLeft>(this);

            _formatter = new ItemDescriptionFormatter(localization);
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
        private readonly ItemDescriptionFormatter _formatter;

        private ItemIdentifier _currentEnteredItemId;

        public void Dispose()
        {
            _localization.LocaleChanged -= OnLocaleChanged;
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus.UnregisterHandler<PickupAreaEntered>(this);
            _bus.UnregisterHandler<PickupAreaLeft>(this);
        }

        public void SetConfig(IEffectListConfig config)
        {
            _formatter.SetConfig(config);
        }

        private void OnLocaleChanged(string locale)
        {
            if (_currentEnteredItemId == default) return;
            Reset(_currentEnteredItemId);
        }

        private void Reset(ItemIdentifier itemId)
        {
            IClientItemConfig clientConfig = _clientDatabase.Get(itemId);
            IItemConfig sharedConfig = _sharedDatabase.Get(itemId);

            ItemDescriptionData data = _formatter.Format(sharedConfig, clientConfig);
            Name.Value = data.Name;
            Description.Value = data.Description;
            TierName.Value = data.TierName;
            Icon.Value = data.ClientConfig.Icon;
            AccentColor.Value = data.ClientConfig.AccentColor;
            Opacity.Value = 1;
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _enteredPickups.Clear();
            _currentEnteredItemId = default;
            Opacity.Value = 0;
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

            _currentEnteredItemId = default;
            Opacity.Value = 0;
        }

        public void Handle(PickupAreaEntered message, IMessageSender sender)
        {
            _currentEnteredItemId = message.ItemId;
            _enteredPickups.Add(message.PickupId, message.ItemId);
            Reset(message.ItemId);
        }
    }
}
