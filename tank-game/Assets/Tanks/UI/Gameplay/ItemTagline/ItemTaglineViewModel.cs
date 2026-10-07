using System;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Gameplay.ItemTagline
{
    public sealed class ItemTaglineViewModel :
        IViewModel, IMessageHandler<ItemPickedUp>, IDisposable
    {
        public ItemTaglineViewModel(
           IMessageBus bus,
           IClientItemDatabase clientItemDatabase,
           ILocalizationDatabase localization)
        {
            _clientItemDatabase = clientItemDatabase;
            _localization = localization;

            _bus = bus;
            _bus.RegisterHandler<ItemPickedUp>(this);
        }

        public BindableProperty<string> Header { get; } = new();
        public BindableProperty<string> Tagline { get; } = new();
        public BindableProperty<Color> AccentColor { get; } = new();
        public BindableProperty<Sprite> Icon { get; } = new();

        private readonly IMessageBus _bus;
        private readonly IClientItemDatabase _clientItemDatabase;
        private readonly ILocalizationDatabase _localization;

        public void Dispose()
        {
            _bus.UnregisterHandler<ItemPickedUp>(this);
        }

        public void Handle(ItemPickedUp message, IMessageSender sender)
        {
            if (message.Owner.Entity.IsPossessed == false) return;

            IClientItemConfig item = _clientItemDatabase.Get(message.ItemId);
            string itemIdString = message.ItemId.ToString();

            _localization.TryGet("Items", "item.tagline-header", out var header);
            _localization.TryGet("Items", $"item.{itemIdString}.tagline", out var tagline);
            _localization.TryGet("Items", $"item.{itemIdString}.name", out var name);

            string accentHex = item.AccentColor.ToHexCode();
            string itemName = string.Format("<color=#{0}>{1}</color>", accentHex, name?.Get());

            header?.AddVariable("item", itemName);
            Header.Value = header?.Get();

            Tagline.Value = ""; // Force refresh
            Tagline.Value = tagline?.Get();

            Icon.Value = item.Icon;
            AccentColor.Value = item.AccentColor;
        }
    }
}
