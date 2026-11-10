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
    public sealed class StoreCounterpartTests : StoreTestsBase
    {
        [Test]
        public void BodyPreviews_UseEquippedTurretAndRefreshWhenEquipmentChanges()
        {
            _data.Apply(new PlayerSnapshot
            {
                Equipped = new EquippedCosmetics { Body = "v3-body", Turret = "v4-turret" }
            });

            CosmeticCardData item = _model.Items.Value.Single();
            Assert.That(item.Body, Is.SameAs(_body));
            Assert.That(item.Turret, Is.SameAs(_turret));

            AddDefaults();
            _data.Apply(new PlayerSnapshot { Equipped = EquippedCosmetics.Default });
            item = _model.Items.Value.Single(x => x.Config.Identifier == "v4-body");

            Assert.That(item.Body, Is.SameAs(_body));
            Assert.That(item.Turret, Is.SameAs(_defaultTurret));
        }

        [Test]
        public void TurretPreviews_UseEquippedBodyAndPreserveCandidateTurret()
        {
            _data.Apply(new PlayerSnapshot
            {
                Equipped = new EquippedCosmetics { Body = "v4-body", Turret = "v3-turret" }
            });

            _model.SelectTab(CosmeticType.Turret);
            CosmeticCardData item = _model.Items.Value.Single();

            Assert.That(item.Body, Is.SameAs(_body));
            Assert.That(item.Turret, Is.SameAs(_turret));
        }

        [Test]
        public void UnknownOrWrongSlotCounterparts_FallBackToV3()
        {
            AddDefaults();
            _data.Apply(new PlayerSnapshot
            {
                Equipped = new EquippedCosmetics { Body = "v4-turret", Turret = "unknown" }
            });

            CosmeticCardData body = _model.Items.Value.Single(x => x.Config.Identifier
                == "v4-body");

            Assert.That(body.Turret, Is.SameAs(_defaultTurret));

            _model.SelectTab(CosmeticType.Turret);
            CosmeticCardData turret = _model.Items.Value.Single(x => x.Config.Identifier
                == "v4-turret");

            Assert.That(turret.Body, Is.SameAs(_defaultBody));
        }

        [Test]
        public async Task ClaimingAndCosmeticActions_HaveIndependentBusyStates()
        {
            using RewardsViewModel rewards = new(_player, _clock, _localization, _dialog);
            TaskCompletionSource<ResultType> claimPending = new();
            TaskCompletionSource<ResultType> equipPending = new();
            _player.ClaimRewardAsync(TimedRewardType.Daily).Returns(claimPending.Task);
            _store.EquipAsync("v4-body").Returns(equipPending.Task);
            _data.Cosmetics.Value.Add("v4-body");

            Task claim = rewards.ClaimAsync(TimedRewardType.Daily);
            Assert.True(rewards.IsBusy.Value);
            Assert.False(_model.IsBusy.Value);

            claimPending.SetResult(Result.Success());
            await claim;
            Task equip = _model.EquipAsync("v4-body");

            Assert.True(_model.IsBusy.Value);
            Assert.False(rewards.IsBusy.Value);
            Assert.True(rewards.Rewards.Value[1].CanClaim);

            equipPending.SetResult(Result.Success());
            await equip;
        }

    }
}
