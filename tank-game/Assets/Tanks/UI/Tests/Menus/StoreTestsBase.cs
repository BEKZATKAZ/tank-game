using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Cosmetics;
using Tanks.Core.Players;
using Tanks.Core.Store;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Rewards;
using Tanks.UI.Menus.Sections;
using Tanks.UI.Menus.Store;
using Tanks.UI.TankModel;
using Tanks.UI.Tests.Helpers;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public abstract class StoreTestsBase
    {
        protected ILocalPlayerService _player;
        protected IStoreService _store;
        protected IServerClock _clock;
        protected ILocalizationDatabase _localization;
        protected IDialogScreenService _dialog;
        protected CosmeticDatabase _database;
        protected CosmeticConfig _body;
        protected CosmeticConfig _turret;
        protected CosmeticConfig _defaultBody;
        protected CosmeticConfig _defaultTurret;
        protected LocalPlayer _data;
        protected StoreViewModel _model;
        protected DateTimeOffset _now;

        [SetUp]
        public void Setup()
        {
            _now = DateTimeOffset.Parse("2026-11-10T00:00:00Z");
            _player = Substitute.For<ILocalPlayerService>();
            _data = new LocalPlayer();
            _data.Apply(new PlayerSnapshot
            {
                Balance = new PlayerBalance { SpareParts = 1000, PridePoints = 200 },
                Rewards = new RewardDeadlines { Playtime = _now.AddSeconds(3661), Daily
                    = _now },
                Equipped = EquippedCosmetics.Default
            });

            _player.LocalPlayer.Returns(_data);
            _player.IsLoaded.Returns(true);
            _player.LoadDataAsync().Returns(Result.Success());
            _clock = Substitute.For<IServerClock>();
            _clock.UtcNow.Returns(_ => _now);
            _store = Substitute.For<IStoreService>();
            _store.IsCatalogLoaded.Returns(true);
            _store.Definitions.Returns(new CosmeticDefinitions
            {
                Items = new Dictionary<string, CosmeticDefinition>
                {
                    ["v4-body"] = new() { Conditions = new UnlockConditions { Prices =
                        new() { ["parts"] = 101, ["pride"] = 300 } } },
                    ["v4-turret"] = new() { Conditions = new UnlockConditions { Prices =
                        new() { ["pride"] = 100 } } }
                },
                Discounts = new Dictionary<string, DiscountDefinition>
                {
                    ["v4-body"] = new() { PercentOff = 25, EndDate = _now.AddHours(1) }
                }
            });

            _localization = Substitute.For<ILocalizationDatabase>();
            _localization.TryGet("Menus", Arg.Any<string>(), out
                Arg.Any<ILocalizedString>()).Returns(call =>
            {
                string key = call.ArgAt<string>(1);
                call[2] = new TestLocalizedString(key == "menu.reward.timer" ?
                    "{hour}h {minutes}m {seconds}s" : key);

                return true;
            });

            _dialog = Substitute.For<IDialogScreenService>();
            _database = ScriptableObject.CreateInstance<CosmeticDatabase>();
            _body = CreateCosmetic("v4-body", CosmeticType.Body);
            _turret = CreateCosmetic("v4-turret", CosmeticType.Turret);
            Set(_database, "_cosmetics", new[] { _body, _turret });
            _model = new StoreViewModel(_player, _store, _clock, _database,
                _localization, _dialog,
                Substitute.For<IMenuSectionService>(), Substitute.For<ITankModelService>());
        }

        [TearDown]
        public void TearDown()
        {
            _model.Dispose();
            UnityEngine.Object.DestroyImmediate(_database);
            UnityEngine.Object.DestroyImmediate(_body);
            UnityEngine.Object.DestroyImmediate(_turret);
            if (_defaultBody) UnityEngine.Object.DestroyImmediate(_defaultBody);
            if (_defaultTurret) UnityEngine.Object.DestroyImmediate(_defaultTurret);
        }

        protected void AddDefaults()
        {
            _defaultBody = CreateCosmetic("v3-body", CosmeticType.Body);
            _defaultTurret = CreateCosmetic("v3-turret", CosmeticType.Turret);
            Set(_database, "_cosmetics", new[] { _body, _turret, _defaultBody,
                _defaultTurret });
        }

        protected static CosmeticConfig CreateCosmetic(string id, CosmeticType type)
        {
            CosmeticConfig config = ScriptableObject.CreateInstance<CosmeticConfig>();
            typeof(PurchasableItem).GetField("_identifier", BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(config, id);

            Set(config, "_cosmeticType", type);
            config.name = id;
            return config;
        }

        protected static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance |
                BindingFlags.NonPublic).SetValue(target, value);
        }

    }
}
