using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Tanks.Client.Effects;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Effects;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.UI.Items;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class ItemDescriptionFormatterTests
    {
        private ILocalizationDatabase _localization;
        private ItemDescriptionFormatter _formatter;
        private IItemConfig _config;
        private IClientItemConfig _client;
        private Dictionary<string, ItemStat> _itemStats;
        private Dictionary<StatType, PlayerStat> _playerStats;

        [SetUp]
        public void Setup()
        {
            _localization = Substitute.For<ILocalizationDatabase>();
            _formatter = new ItemDescriptionFormatter(_localization);
            _config = Substitute.For<IItemConfig>();
            _client = Substitute.For<IClientItemConfig>();
            _itemStats = new Dictionary<string, ItemStat>();
            _playerStats = new Dictionary<StatType, PlayerStat>();
            _config.Identifier.Returns(new ItemIdentifier("test"));
            _config.Tier.Returns(ItemTier.Epic);
            _config.ItemStats.Returns(_itemStats);
            _config.PlayerStats.Returns(_playerStats);
        }

        private ILocalizedString Text(string table, string key, string value)
        {
            var localized = Substitute.For<ILocalizedString>();
            localized.Get().Returns(value);
            _localization.TryGet(table, key, out Arg.Any<ILocalizedString>()).Returns(call =>
            {
                call[2] = localized;
                return true;
            });
            return localized;
        }

        [Test]
        public void Format_ReturnsLocalizedTextAndOriginalVisualConfig()
        {
            Text("Items", "item.test.name", "Test Item");
            Text("Items", "item.test.description", "Description");
            Text("Items", "item.tier.epic", "Epic");

            ItemDescriptionData result = _formatter.Format(_config, _client);

            Assert.AreEqual("Test Item", result.Name);
            Assert.AreEqual("Description", result.Description);
            Assert.AreEqual("Epic", result.TierName);
            Assert.AreSame(_client, result.ClientConfig);
        }

        [TestCase(12.5f, StatValueType.Percent, "<color=#0f0><b>12.5%</b></color>")]
        [TestCase(-2.25f, StatValueType.Flat, "<color=#f00><b>-2.25</b></color>")]
        public void Format_ItemStats_UsesSignedColorsAndInvariantNumbers(
            float value, StatValueType type, string expected)
        {
            var description = Text("Items", "item.test.description", "Description");
            _itemStats["amount"] = new ItemStat { Value = value, ValueType = type };

            _formatter.Format(_config, _client);

            description.Received().AddVariable("amount", expected);
        }

        [Test]
        public void Format_PlayerStats_AppendsLocalizedStatsAndDuration()
        {
            Text("Items", "item.test.description", "Description");
            var stat = Text("Stats", "stat.rate-of-fire", "Rate +25%");
            var duration = Text("Stats", "stat.duration-seconds", "3 seconds");
            _playerStats[StatType.RateOfFire] = new PlayerStat
            {
                Value = 25, ValueType = StatValueType.Percent, Duration = 3
            };

            ItemDescriptionData result = _formatter.Format(_config, _client);

            Assert.AreEqual("Description\n\nRate +25% (3 seconds)", result.Description);
            stat.Received().AddVariable("value", "<color=#0f0>+25%</color>");
            duration.Received().AddVariable("seconds", 3f);
        }

        [Test]
        public void Format_MissingLocalization_UsesEmptyTextAndStatNameFallback()
        {
            _playerStats[StatType.Damage] = new PlayerStat { Value = 5 };

            ItemDescriptionData result = _formatter.Format(_config, _client);

            Assert.AreEqual("", result.Name);
            Assert.AreEqual("", result.TierName);
            Assert.AreEqual("Damage", result.Description);
        }

        [Test]
        public void Format_Effects_UsesLocalizedNameAndAccentMarkup()
        {
            var description = Text("Items", "item.test.description", "Description");
            Text("Gameplay", "effect.test", "Chilled");
            var effect = new EffectConfig();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(EffectConfig).GetField("_entryName", flags).SetValue(effect, "effect.test");
            typeof(EffectConfig).GetField("_localizedFormatName", flags).SetValue(effect, "effect");
            typeof(EffectConfig).GetField("_hexColor", flags).SetValue(effect, "00ffff");

            var effects = Substitute.For<IEffectListConfig>();
            effects.Effects.Returns(new Dictionary<EffectType, EffectConfig> { [default] = effect });
            _formatter.SetConfig(effects);
            _formatter.Format(_config, _client);

            description.Received().AddVariable("effect", "<color=#00ffff><b>«Chilled»</b></color>");
        }
    }
}
