using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tanks.Client.Effects;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.UI.Items
{
    public sealed class ItemDescriptionFormatter
    {
        public ItemDescriptionFormatter(ILocalizationDatabase localization)
        {
            _localization = localization;
        }

        private readonly ILocalizationDatabase _localization;
        private IEffectListConfig _config;

        public void SetConfig(IEffectListConfig config) => _config = config;

        public ItemDescriptionData Format(IItemConfig sharedConfig, IClientItemConfig clientConfig)
        {
            string idString = sharedConfig.Identifier.ToString();
            string tierString = sharedConfig.Tier.ToString().ToLowerInvariant();
            string playerStats = FormatPlayerStats(sharedConfig.PlayerStats);

            _localization.TryGet("Items", $"item.{idString}.name", out ILocalizedString name);
            _localization.TryGet("Items", $"item.tier.{tierString}", out ILocalizedString tier);

            if (_localization.TryGet(
                "Items", $"item.{idString}.description", out ILocalizedString description))
            {
                FormatEffects(description);

                foreach ((string statName, ItemStat stat) in sharedConfig.ItemStats)
                {
                    FormatItemStat(statName, stat, description);
                }

                if (string.IsNullOrWhiteSpace(playerStats) == false)
                {
                    // Separate player stats from the description.
                    playerStats = $"\n\n{playerStats}";
                }
            }

            return new ItemDescriptionData(sharedConfig, clientConfig,
                name?.Get() ?? "", (description?.Get() ?? "") + playerStats, tier?.Get() ?? "");
        }

        private void FormatItemStat(string name, ItemStat stat, ILocalizedString description)
        {
            string color = stat.Value switch
            {
                < 0 => "#f00",
                _ => "#0f0"
            };

            string value = stat.Value.ToString("0.##;-0.##;0", CultureInfo.InvariantCulture);

            if (stat.Type is ItemStatType.Percent)
            {
                _localization.TryGet("Stats", "stat.percent", out var percent);
                percent?.AddVariable("value", value);
                value = percent?.Get() ?? (value + '%');
            }

            else if (stat.Type is ItemStatType.Seconds)
            {
                bool isPlural = stat.Value > 1;
                string secondsEntry = $"stat.seconds-{(isPlural ? "plural" : "singular")}";

                _localization.TryGet("Stats", secondsEntry, out var seconds);
                seconds?.AddVariable("value", value);

                value = seconds?.Get() ?? value;
                color = "#ff0";
            }

            string formatted = $"<color={color}><b>{value}</b></color>";
            description.AddVariable(name, formatted);
        }

        private string FormatPlayerStats(IReadOnlyDictionary<StatType, PlayerStat> stats)
        {
            if (stats is null || stats.Count == 0) return "";

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
                string value = stat.Value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);

                // For example +150%, -150%
                if (stat.ValueType is StatValueType.Percent)
                {
                    _localization.TryGet("Stats", "stat.percent", out var percent);
                    percent?.AddVariable("value", value);
                    value = percent?.Get() ?? (value + '%');
                }

                _localization.TryGet("Stats", statEntry, out var statLocalization);
                string formattedValue = $"<color={color}>{value}</color>";

                statLocalization?.AddVariable("value", formattedValue);
                builder.Append(statLocalization?.Get() ?? statType);

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

        private void FormatEffects(ILocalizedString description)
        {
            if (_config is null || _config.Effects is not { Count: > 0 }) return;

            foreach (EffectConfig effect in _config.Effects.Values)
            {
                _localization.TryGet(
                    "Gameplay", effect.EntryName, out ILocalizedString effectName);

                string effectNameText =
                    $"<color=#{effect.Color}><b>«{effectName?.Get() ?? ""}»</b></color>";

                description.AddVariable(effect.LocalizedFormatName, effectNameText);
            }
        }
    }
}
