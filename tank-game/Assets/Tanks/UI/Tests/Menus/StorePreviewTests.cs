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
using Tanks.UI.TankModel;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public sealed class StorePreviewTests
    {
        private readonly List<CosmeticConfig> _configs = new();
        private CosmeticDatabase _database;
        private LocalPlayer _data;
        private IStoreService _store;
        private ITankModelService _tanks;
        private IDialogScreenService _dialog;
        private IMenuSectionService _navigation;
        private StoreViewModel _model;

        [SetUp]
        public void Setup()
        {
            _database = ScriptableObject.CreateInstance<CosmeticDatabase>();
            var definitions = new Dictionary<string, CosmeticDefinition>();

            foreach (string id in new[] { "v3-body", "a-body", "b-body", "v3-turret", "a-turret", "b-turret" })
            {
                var config = ScriptableObject.CreateInstance<CosmeticConfig>();
                Set(config, typeof(PurchasableItem), "_identifier", id);
                Set(config, typeof(CosmeticConfig), "_cosmeticType",
                    id.EndsWith("-body") ? CosmeticType.Body : CosmeticType.Turret);

                config.name = id;
                _configs.Add(config);
                definitions[id] = new CosmeticDefinition
                {
                    Conditions = new UnlockConditions { Prices = new() { ["parts"] = 100 } }
                };
            }

            Set(_database, typeof(CosmeticDatabase), "_cosmetics", _configs.ToArray());
            _data = new LocalPlayer();
            _data.Apply(new PlayerSnapshot
            {
                Balance = new PlayerBalance { SpareParts = 1000 },
                Equipped = EquippedCosmetics.Default
            });

            var player = Substitute.For<ILocalPlayerService>();
            player.LocalPlayer.Returns(_data);
            player.IsLoaded.Returns(true);
            _store = Substitute.For<IStoreService>();
            _store.IsCatalogLoaded.Returns(true);
            _store.Definitions.Returns(new CosmeticDefinitions { Items = definitions });
            _tanks = Substitute.For<ITankModelService>();
            _dialog = Substitute.For<IDialogScreenService>();
            _navigation = Substitute.For<IMenuSectionService>();
            _model = new StoreViewModel(player, _store, Substitute.For<IServerClock>(), _database,
                Substitute.For<ILocalizationDatabase>(), _dialog, _navigation, _tanks);

            _tanks.ClearReceivedCalls();
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
        public void Preview_CombinesSelectionsWithoutChangingSavedEquipment()
        {
            _model.PreviewCosmetic("a-body");
            _model.SelectTab(CosmeticType.Turret);

            Assert.That(_model.Items.Value.All(item => item.Body.Identifier == "a-body"), Is.True);
            Assert.That(_model.Items.Value.Single(item => item.Config.Identifier == "b-turret").Turret.Identifier,
                Is.EqualTo("b-turret"));

            _model.PreviewCosmetic("a-turret");
            _model.SelectTab(CosmeticType.Body);

            Assert.That(_model.Items.Value.All(item => item.Turret.Identifier == "a-turret"), Is.True);
            Assert.That(_model.Items.Value.Single(item => item.Config.Identifier == "b-body").Body.Identifier,
                Is.EqualTo("b-body"));

            _tanks.Received().PreviewCosmetics(0, Arg.Is<EquippedCosmetics>(value =>
                value.Body == "a-body" && value.Turret == "a-turret"));

            Assert.That(_data.Equipped.Value.Body, Is.EqualTo("v3-body"));
            Assert.That(_data.Cosmetics.Value, Is.Empty);
            _ = _store.DidNotReceiveWithAnyArgs().EquipAsync(default);
            _ = _store.DidNotReceiveWithAnyArgs().PurchaseAsync(default, default, default);
        }

        [Test]
        public void BackWithoutBuying_ClearsBothSelectionsWithoutSaving()
        {
            _model.PreviewCosmetic("a-body");
            _model.PreviewCosmetic("a-turret");
            _model.Back();

            _tanks.Received(1).ClearCosmeticPreview(0);
            Assert.That(_data.Equipped.Value.Body, Is.EqualTo("v3-body"));
            Assert.That(_data.Equipped.Value.Turret, Is.EqualTo("v3-turret"));
            _ = _store.DidNotReceiveWithAnyArgs().EquipAsync(default);
            _ = _store.DidNotReceiveWithAnyArgs().PurchaseAsync(default, default, default);
        }

        [Test]
        public void BalanceRefresh_DoesNotDiscardTemporarySelections()
        {
            _model.PreviewCosmetic("a-body");
            _tanks.ClearReceivedCalls();
            _data.Balance.Value = new PlayerBalance { SpareParts = 2000 };
            _data.Revision.Value++;

            _tanks.Received().PreviewCosmetics(0, Arg.Is<EquippedCosmetics>(value => value.Body == "a-body"));
            _tanks.DidNotReceive().ClearCosmeticPreview(0);
        }

        [Test]
        public async Task Buying_UnlocksWithoutEquippingAndBackClearsPreview()
        {
            _model.PreviewCosmetic("a-body");
            Action confirm = null;
            _dialog.When(x => x.ShowPurchaseConfirmation(
                Arg.Any<string>(), Arg.Any<string>(), 100, Arg.Any<Action>()))
                .Do(call => confirm = call.Arg<Action>());

            _store.PurchaseAsync("a-body", "parts", 100).Returns(_ =>
            {
                _data.Cosmetics.Value.Add("a-body");
                _data.Revision.Value++;
                return Result.Success();
            });

            _model.ConfirmPurchase("a-body", "parts");
            Assert.NotNull(confirm);
            confirm();
            await Task.Yield();
            _model.Back();

            Assert.That(_data.Equipped.Value.Body, Is.EqualTo("v3-body"));
            _tanks.Received(1).ClearCosmeticPreview(0);
            _ = _store.DidNotReceiveWithAnyArgs().EquipAsync(default);
            _navigation.Received(1).OpenSection(MenuSectionType.Main, false);
        }

        [Test]
        public async Task SuccessfulEquip_ReplacesOnlyItsPreviewSlot()
        {
            _model.PreviewCosmetic("a-body");
            _model.PreviewCosmetic("a-turret");
            _data.Cosmetics.Value.Add("b-body");
            _store.EquipAsync("b-body").Returns(_ =>
            {
                _data.Equipped.Value = new EquippedCosmetics { Body = "b-body", Turret = "v3-turret" };
                _data.Revision.Value++;
                return Result.Success();
            });

            await _model.EquipAsync("b-body");

            _tanks.Received().PreviewCosmetics(0, Arg.Is<EquippedCosmetics>(value =>
                value.Body == "b-body" && value.Turret == "a-turret"));

            _model.Back();
            Assert.That(_data.Equipped.Value.Body, Is.EqualTo("b-body"));
            _tanks.Received().ClearCosmeticPreview(0);
        }

        [Test]
        public void UnknownSavedCounterpart_FallsBackToDefaultAndInvalidSelectionIsIgnored()
        {
            _data.Equipped.Value = new EquippedCosmetics { Body = "unknown", Turret = "unknown" };
            _data.Revision.Value++;
            _tanks.ClearReceivedCalls();
            _model.PreviewCosmetic("missing");
            _tanks.DidNotReceiveWithAnyArgs().PreviewCosmetics(default, default);

            _model.PreviewCosmetic("a-body");
            _tanks.Received().PreviewCosmetics(0, Arg.Is<EquippedCosmetics>(value =>
                value.Body == "a-body" && value.Turret == "v3-turret"));
        }

        [Test]
        public void Disposal_ClearsPreviewAndStopsReactingToSnapshots()
        {
            _model.PreviewCosmetic("a-body");
            _model.Dispose();
            _tanks.Received(1).ClearCosmeticPreview(0);
            _tanks.ClearReceivedCalls();
            _data.Revision.Value++;
            _model.PreviewCosmetic("b-body");

            _tanks.DidNotReceiveWithAnyArgs().PreviewCosmetics(default, default);
        }

        private static void Set(object target, Type type, string field, object value)
        {
            type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
