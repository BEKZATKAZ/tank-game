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
    public sealed class StoreTests : StoreTestsBase
    {
        [Test]
        public void Discount_RoundsUpAndExpiresWithoutChangingBasePrice()
        {
            PurchaseOption price = _model.Items.Value.Single().Prices[0];
            Assert.That(price.Price, Is.EqualTo(76));
            Assert.That(price.OriginalPrice, Is.EqualTo(101));
            Assert.True(price.CanAfford);
            Assert.False(_model.Items.Value.Single().Prices[1].CanAfford);

            _now = _now.AddHours(1);
            _model.Tick();

            Assert.That(_model.Items.Value.Single().Prices[0].Price, Is.EqualTo(101));
            Assert.That(_model.Items.Value.Single().PercentOff, Is.Zero);
        }

        [Test]
        public void Tabs_PrideOnlyItemsAndOwnedEquipment()
        {
            _model.SelectTab(CosmeticType.Turret);
            Assert.That(_model.Items.Value.Single().Prices.Single().Currency,
                Is.EqualTo("pride"));

            _data.Apply(new PlayerSnapshot
            {
                Cosmetics = new List<string> { "v4-turret" },
                Equipped = new EquippedCosmetics { Body = "v3-body", Turret = "v4-turret" }
            });

            Assert.True(_model.Items.Value.Single().IsOwned);
            Assert.True(_model.Items.Value.Single().IsEquipped);
        }

        [Test]
        public async Task Purchase_RequiresConfirmationAndSuppressesDuplicateRequests()
        {
            Action confirm = null;
            _dialog.When(x => x.ShowPurchaseConfirmation(Arg.Any<string>(),
                Arg.Any<string>(), 76, Arg.Any<Action>()))
                .Do(call => confirm = call.Arg<Action>());
            TaskCompletionSource<ResultType> pending = new();
            _store.PurchaseAsync("v4-body", "parts", 76).Returns(pending.Task);

            _model.ConfirmPurchase("v4-body", "parts");
            _ = _store.DidNotReceiveWithAnyArgs().PurchaseAsync(default, default, default);
            Assert.NotNull(confirm);
            confirm();
            confirm();
            _ = _store.Received(1).PurchaseAsync("v4-body", "parts", 76);
            Assert.True(_model.IsBusy.Value);

            pending.SetResult(Result.Success());
            await Task.Yield();
        }

        [Test]
        public async Task CatalogFailure_LeavesRewardsAvailableAndCanRetry()
        {
            using RewardsViewModel rewards = new(_player, _clock, _localization, _dialog);
            _store.LoadCatalogAsync().Returns(Result.Failure("unknown"), Result.Success());
            await _model.InitializeAsync();
            Assert.True(_model.CatalogError.Value);
            Assert.True(rewards.Rewards.Value[1].CanClaim);

            await _model.InitializeAsync();
            Assert.False(_model.CatalogError.Value);
        }

        [Test]
        public async Task FailedMutation_RefreshesSnapshotAndShowsError()
        {
            _data.Cosmetics.Value.Add("v4-body");
            _store.EquipAsync("v4-body").Returns(Result.Failure("not-unlocked"));
            await _model.EquipAsync("v4-body");

            await _player.Received(1).LoadDataAsync();
            _dialog.Received(1).ShowErrorFromCode("store", "not-unlocked");
            Assert.False(_model.IsBusy.Value);
        }

        [Test]
        public void SwitchingTabs_ReusesCachedRowsAndPricesWithoutBackendRequests()
        {
            CosmeticCardData[] bodies = _model.Items.Value;
            PurchaseOption[] prices = bodies[0].Prices;
            _store.ClearReceivedCalls();

            _model.SelectTab(CosmeticType.Turret);
            CosmeticCardData[] turrets = _model.Items.Value;
            _model.SelectTab(CosmeticType.Body);

            Assert.That(_model.Items.Value, Is.SameAs(bodies));
            Assert.That(_model.Items.Value[0].Prices, Is.SameAs(prices));
            _model.SelectTab(CosmeticType.Turret);
            Assert.That(_model.Items.Value, Is.SameAs(turrets));
            _ = _store.DidNotReceive().LoadCatalogAsync();
        }

        [Test]
        public async Task PriceMismatch_RefreshesCatalogWithoutReloadingPlayerData()
        {
            Action confirm = null;
            _dialog.When(value => value.ShowPurchaseConfirmation(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Action>()))
                .Do(call => confirm = call.Arg<Action>());

            _store.PurchaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(Result.Failure("price-changed"));

            _store.RefreshCatalogAsync().Returns(Result.Success());
            PurchaseOption price = _model.Items.Value[0].Prices[0];
            _model.ConfirmPurchase(_model.Items.Value[0].Config.Identifier, price.Currency);
            confirm();
            await Task.Yield();

            _ = _store.Received(1).RefreshCatalogAsync();
            _ = _player.DidNotReceive().LoadDataAsync();
        }

    }
}
