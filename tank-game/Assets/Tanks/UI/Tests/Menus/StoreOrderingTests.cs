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
using Tanks.UI.Menus.Sections;
using Tanks.UI.Menus.Store;
using UnityEngine;
using Tanks.UI.TankModel;

namespace Tanks.UI.Tests.Menus
{
    public sealed class StoreOrderingTests
    {
        private readonly List<CosmeticConfig> _configs = new();
        private CosmeticDatabase _database;
        private LocalPlayer _data;
        private IStoreService _store;
        private IDialogScreenService _dialog;
        private CosmeticDefinitions _definitions;
        private StoreViewModel _model;
        private DateTimeOffset _now;

        [SetUp]
        public void Setup()
        {
            _now = DateTimeOffset.Parse("2026-11-10T00:00:00Z");
            _data = new LocalPlayer();
            _data.Balance.Value = new PlayerBalance { SpareParts = 10000 };
            var player = Substitute.For<ILocalPlayerService>();
            player.LocalPlayer.Returns(_data);
            player.IsLoaded.Returns(true);
            var clock = Substitute.For<IServerClock>();
            clock.UtcNow.Returns(_ => _now);
            _definitions = new CosmeticDefinitions { Items = new(), Discounts = new() };
            _store = Substitute.For<IStoreService>();
            _store.IsCatalogLoaded.Returns(true);
            _store.Definitions.Returns(_ => _definitions);
            _store.LoadCatalogAsync().Returns(Result.Success());
            _dialog = Substitute.For<IDialogScreenService>();
            _database = ScriptableObject.CreateInstance<CosmeticDatabase>();
            SetConfigs();

            _model = new StoreViewModel(player, _store, clock, _database,
                Substitute.For<ILocalizationDatabase>(), _dialog, Substitute.For<IMenuSectionService>(),
                Substitute.For<ITankModelService>());
        }

        [TearDown]
        public void TearDown()
        {
            _model.Dispose();
            UnityEngine.Object.DestroyImmediate(_database);
            foreach (CosmeticConfig config in _configs) UnityEngine.Object.DestroyImmediate(config);
            _configs.Clear();
        }

        [Test]
        public void OwnedItems_PrecedeCheaperUnownedItemsAndKeepCurrencyOrder()
        {
            Add("cheap-body", 1, 1);
            Add("owned-pride-body", null, 2, owned: true);
            Add("owned-parts-body", 1000, 100, owned: true);
            Refresh();

            AssertOrder("owned-parts-body", "owned-pride-body", "cheap-body");
        }

        [Test]
        public void PartsPrices_SortBeforePrideOnlyPricesWithoutComparingCurrencies()
        {
            Add("pride-expensive-body", null, 200);
            Add("parts-expensive-body", 1000, 1);
            Add("pride-cheap-body", null, 1);
            Add("parts-cheap-body", 10, 900);
            Refresh();

            AssertOrder("parts-cheap-body", "parts-expensive-body", "pride-cheap-body", "pride-expensive-body");
        }

        [Test]
        public void Discounts_ChangeOrderingAndExpiryRestoresBasePriceOrder()
        {
            Add("discounted-body", 100, 10);
            Add("regular-body", 80, 8);
            _definitions.Discounts["discounted-body"] = new DiscountDefinition
            {
                PercentOff = 25,
                EndDate = _now.AddHours(1)
            };

            Refresh();
            AssertOrder("discounted-body", "regular-body");

            _now = _now.AddHours(1);
            _model.Tick();

            AssertOrder("regular-body", "discounted-body");
        }

        [Test]
        public void EqualPrices_UseIdentifierAsStableTieBreaker()
        {
            Add("z-body", 100, 10);
            Add("a-body", 100, 20);
            Refresh();

            AssertOrder("a-body", "z-body");
        }

        [Test]
        public void DefaultsAreFreeAndMissingCatalogPricesGoLastAmongOwnedItems()
        {
            Add("unknown-body", null, null, owned: true);
            Add("owned-pride-body", null, 10, owned: true);
            Add("unowned-body", 1, 1);
            Add("v3-body", null, null);
            Add("owned-parts-body", 10, 10, owned: true);
            Refresh();

            AssertOrder("v3-body", "owned-parts-body", "owned-pride-body", "unknown-body", "unowned-body");
        }

        [Test]
        public async Task PurchaseSnapshot_MovesNewlyOwnedItemToTheFront()
        {
            Add("cheap-body", 10, 1);
            Add("expensive-body", 100, 10);
            Refresh();
            Action confirm = null;
            _dialog.When(x => x.ShowPurchaseConfirmation(
                Arg.Any<string>(), Arg.Any<string>(), 100, Arg.Any<Action>()))
                .Do(call => confirm = call.Arg<Action>());

            _store.PurchaseAsync("expensive-body", "parts", 100).Returns(_ =>
            {
                _data.Cosmetics.Value.Add("expensive-body");
                _data.Revision.Value++;
                return Result.Success();
            });

            _model.ConfirmPurchase("expensive-body", "parts");
            Assert.NotNull(confirm);
            confirm();
            await Task.Yield();

            AssertOrder("expensive-body", "cheap-body");
        }

        [Test]
        public async Task CatalogRefresh_ReordersChangedPrices()
        {
            Add("a-body", 100, 1);
            Add("b-body", 200, 2);
            Refresh();
            _definitions.Items["b-body"] = Definition(50, 2);

            await _model.InitializeAsync();

            AssertOrder("b-body", "a-body");
        }

        private void Add(string identifier, int? parts, int? pride, bool owned = false)
        {
            var config = ScriptableObject.CreateInstance<CosmeticConfig>();
            typeof(PurchasableItem).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(config, identifier);

            config.name = identifier;
            _configs.Add(config);
            _definitions.Items[identifier] = Definition(parts, pride);
            if (owned) _data.Cosmetics.Value.Add(identifier);
        }

        private static CosmeticDefinition Definition(int? parts, int? pride)
        {
            var prices = new Dictionary<string, int>();
            if (parts.HasValue) prices["parts"] = parts.Value;
            if (pride.HasValue) prices["pride"] = pride.Value;
            return new CosmeticDefinition { Conditions = new UnlockConditions { Prices = prices } };
        }

        private void SetConfigs()
        {
            typeof(CosmeticDatabase).GetField("_cosmetics", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_database, _configs.ToArray());
        }

        private void Refresh()
        {
            SetConfigs();
            _model.SelectTab(CosmeticType.Body);
        }

        private void AssertOrder(params string[] identifiers)
        {
            Assert.That(_model.Items.Value.Select(item => item.Config.Identifier), Is.EqualTo(identifiers));
        }
    }
}
