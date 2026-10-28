using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Localization;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.Shared.Stats;
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

        public void Dispose()
        {
            _localization.LocaleChanged -= OnLocaleChanged;
            _bus.UnregisterHandler<PickupAreaEntered>(this);
            _bus.UnregisterHandler<PickupAreaLeft>(this);
        }

        private void OnLocaleChanged(string locale)
        {
            if (_currentEnteredItemId == default) return;
            Reset(_currentEnteredItemId);
        }

        private void FormatStat(string name, ItemStat stat, ILocalizedString description)
        {
            string color = stat.Value switch
            {
                < 0 => "#f00",
                _ => "#0f0"
            };

            string value = stat.Value.ToString("0.##;-0.##;0", CultureInfo.InvariantCulture);
            if (stat.ValueType is StatValueType.Percent) value += '%';

            string formatted = $"<color={color}><b>{value}</b></color>";
            description.AddVariable(name, formatted);
        }

        private string FormatPlayerStats(IReadOnlyDictionary<StatType, PlayerStat> stats)
        {
            if (stats.Any() == false) return "";

            StringBuilder builder = new StringBuilder();

            foreach ((StatType type, PlayerStat stat) in stats)
            {
                if (builder.Length > 0) builder.Append('\n');

                string color = stat.Value switch
                {
                    < 0 => "#f00",
                    _ => "#0f0"
                };

                string statType = type.ToString();
                string statEntry = $"stat.{ToKebabCase(statType)}";

                _localization.TryGet("Stats", statEntry, out var name);

                string statName = name?.Get() ?? statType;
                string value = stat.Value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);

                // For example +150%, -150%
                if (stat.ValueType is StatValueType.Percent)
                {
                    value += '%';
                }

                builder.Append($"{statName}: <color={color}><b>{value}</b></color>");

                if (stat.Duration < 0 || _localization.TryGet(
                    "Stats", "stat.duration-seconds", out var duration) == false) continue;

                duration.AddVariable("seconds", stat.Duration);
                builder.Append($" ({duration.Get()})");
            }

            return builder.ToString();
        }

        private static string ToKebabCase(string value)
        {
            value = Regex.Replace(value, @"([A-Z]+)([A-Z][a-z])", "$1-$2");
            value = Regex.Replace(value, @"([a-z\d])([A-Z])", "$1-$2");
            return value.ToLowerInvariant();
        }

        private void Reset(ItemIdentifier itemId)
        {
            IClientItemConfig clientConfig = _clientDatabase.Get(itemId);
            IItemConfig sharedConfig = _sharedDatabase.Get(itemId);

            string idString = itemId.ToString();
            string tierString = sharedConfig.Tier.ToString().ToLower();
            string playerStats = FormatPlayerStats(sharedConfig.PlayerStats);

            _localization.TryGet("Items", $"item.{idString}.name", out ILocalizedString name);
            _localization.TryGet("Items", $"item.tier.{tierString}", out ILocalizedString tier);

            if (_localization.TryGet(
                "Items", $"item.{idString}.description", out ILocalizedString description))
            {
                foreach ((string statName, ItemStat stat) in sharedConfig.ItemStats)
                {
                    FormatStat(statName, stat, description);
                }

                if (string.IsNullOrWhiteSpace(playerStats) == false)
                {
                    // Add empty lines for more visiblity
                    playerStats = $"\n\n{playerStats}";
                }
            }

            Name.Value = name?.Get() ?? "";
            Description.Value = (description?.Get() ?? "") + playerStats;
            TierName.Value = tier?.Get() ?? "";

            Icon.Value = clientConfig.Icon;
            AccentColor.Value = clientConfig.AccentColor;
            Opacity.Value = 1;
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _enteredPickups.Clear();
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
