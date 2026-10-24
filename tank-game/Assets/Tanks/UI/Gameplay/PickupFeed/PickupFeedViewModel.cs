using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Gameplay.PickupFeed
{
    public sealed class PickupFeedEntry
    {
        public PickupFeedEntry(string player, string pickedUp, Sprite icon, Color accentColor)
        {
            Player = player;
            PickedUp = pickedUp;
            Icon = icon;
            AccentColor = accentColor;
        }

        public string Player { get; }
        public string PickedUp { get; }
        public Sprite Icon { get; }
        public Color AccentColor { get; }
    }

    public sealed class PickupFeedViewModel : IViewModel, IMessageHandler<ItemPickedUp>, IDisposable
    {
        public PickupFeedViewModel(
            IMessageBus bus, IClientItemDatabase clientItemDatabase, ILocalizationDatabase localization)
        {
            _bus = bus;
            _clientItemDatabase = clientItemDatabase;
            _localization = localization;

            _bus.RegisterHandler<ItemPickedUp>(this);
        }

        public BindableProperty<IReadOnlyList<PickupFeedEntry>> Entries { get; } =
            new(Array.Empty<PickupFeedEntry>());

        private readonly IMessageBus _bus;
        private readonly IClientItemDatabase _clientItemDatabase;
        private readonly ILocalizationDatabase _localization;
        private readonly List<(PickupFeedEntry Entry, float RemainingSeconds)> _entries = new();

        public void Dispose()
        {
            _bus.UnregisterHandler<ItemPickedUp>(this);
        }

        public void Handle(ItemPickedUp message, IMessageSender sender)
        {
            var participant = message.Owner?.Entity?.GetBehaviour<ITeamParticipantBehaviour>();
            if (participant?.State is null) return;

            IClientItemConfig item = _clientItemDatabase.Get(message.ItemId);
            _localization.TryGet("Items", "item.picked-up", out var pickedUp);

            PickupFeedEntry entry = new PickupFeedEntry(
                participant.State.DisplayName, pickedUp?.Get() ?? "picked up", item.Icon, item.AccentColor);

            _entries.Insert(0, (entry, 5));
            if (_entries.Count > 5) _entries.RemoveAt(_entries.Count - 1);

            RefreshEntries();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0) return;

            bool changed = false;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.RemainingSeconds -= deltaTime;

                if (entry.RemainingSeconds > 0)
                {
                    _entries[i] = entry;
                    continue;
                }

                _entries.RemoveAt(i);
                changed = true;
            }

            if (changed) RefreshEntries();
        }

        private void RefreshEntries()
        {
            Entries.Value = _entries.Select(x => x.Entry).ToArray();
        }
    }
}
