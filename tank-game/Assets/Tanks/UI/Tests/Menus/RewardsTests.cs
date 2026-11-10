using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Localization;
using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Rewards;
using Tanks.UI.Tests.Helpers;

namespace Tanks.UI.Tests.Menus
{
    public sealed class RewardsTests
    {
        private ILocalPlayerService _player;
        private IServerClock _clock;
        private ILocalizationDatabase _localization;
        private IDialogScreenService _dialog;
        private LocalPlayer _data;
        private RewardsViewModel _model;
        private DateTimeOffset _now;

        [SetUp]
        public void Setup()
        {
            _now = DateTimeOffset.Parse("2026-11-10T00:00:00Z");
            _player = Substitute.For<ILocalPlayerService>();
            _data = new LocalPlayer();
            _data.Apply(new PlayerSnapshot
            {
                Rewards = new RewardDeadlines { Playtime = _now.AddSeconds(3661), Daily = _now }
            });

            _player.LocalPlayer.Returns(_data);
            _player.IsLoaded.Returns(true);
            _player.LoadDataAsync().Returns(Result.Success());
            _clock = Substitute.For<IServerClock>();
            _clock.UtcNow.Returns(_ => _now);
            _localization = Substitute.For<ILocalizationDatabase>();
            _localization.TryGet("Menus", Arg.Any<string>(), out Arg.Any<ILocalizedString>()).Returns(call =>
            {
                string key = call.ArgAt<string>(1);
                call[2] = new TestLocalizedString(key == "menu.reward.timer" ? "{hour}h {minutes}m {seconds}s" : key);
                return true;
            });

            _dialog = Substitute.For<IDialogScreenService>();
            _model = new RewardsViewModel(_player, _clock, _localization, _dialog);
        }

        [TearDown]
        public void TearDown() => _model.Dispose();

        [Test]
        public void Countdown_EnablesClaimAtExactDeadlineAndHidesReadyTimer()
        {
            Assert.That(_model.Rewards.Value[0].Timer, Is.EqualTo("1h 1m 1s"));
            Assert.False(_model.Rewards.Value[0].CanClaim);
            Assert.True(_model.Rewards.Value[1].CanClaim);

            _now = _now.AddSeconds(3661);
            _model.Tick();

            Assert.That(_model.Rewards.Value[0].Timer, Is.Empty);
            Assert.True(_model.Rewards.Value[0].CanClaim);
        }

        [Test]
        public async Task Claim_RejectsCooldownAndPreventsDuplicateOrOverlappingClaims()
        {
            await _model.ClaimAsync(TimedRewardType.Playtime);
            _ = _player.DidNotReceive().ClaimRewardAsync(TimedRewardType.Playtime);
            var pending = new TaskCompletionSource<ResultType>();
            _player.ClaimRewardAsync(TimedRewardType.Daily).Returns(pending.Task);

            Task first = _model.ClaimAsync(TimedRewardType.Daily);
            await _model.ClaimAsync(TimedRewardType.Daily);
            _ = _player.Received(1).ClaimRewardAsync(TimedRewardType.Daily);
            Assert.False(_model.Rewards.Value[1].CanClaim);
            Assert.True(_model.IsBusy.Value);

            pending.SetResult(Result.Success());
            await first;
            Assert.False(_model.IsBusy.Value);
        }

        [Test]
        public async Task FailedClaim_RefreshesSnapshotBeforeShowingError()
        {
            _player.ClaimRewardAsync(TimedRewardType.Daily).Returns(Result.Failure("already-claimed"));
            _player.LoadDataAsync().Returns(_ =>
            {
                _data.Apply(new PlayerSnapshot
                {
                    Rewards = new RewardDeadlines { Playtime = _now.AddHours(3), Daily = _now.AddDays(1) }
                });

                return Result.Success();
            });

            await _model.ClaimAsync(TimedRewardType.Daily);

            _ = _player.Received(1).LoadDataAsync();
            _dialog.Received(1).ShowErrorFromCode("store", "already-claimed");
            Assert.False(_model.Rewards.Value[1].CanClaim);
            Assert.False(_model.IsBusy.Value);
        }

        [Test]
        public async Task SuccessfulClaim_UsesSnapshotDeadlinesAndBalance()
        {
            _player.ClaimRewardAsync(TimedRewardType.Daily).Returns(_ =>
            {
                _data.Apply(new PlayerSnapshot
                {
                    Balance = new PlayerBalance { SpareParts = 1000 },
                    Rewards = new RewardDeadlines { Playtime = _now.AddHours(3), Daily = _now.AddDays(1) }
                });

                return Result.Success();
            });

            await _model.ClaimAsync(TimedRewardType.Daily);

            Assert.That(_data.Balance.Value.SpareParts, Is.EqualTo(1000));
            Assert.False(_model.Rewards.Value[1].CanClaim);
            Assert.That(_model.Rewards.Value[1].Timer, Is.EqualTo("24h 0m 0s"));
        }

        [Test]
        public async Task MissingPlayerDataAndInvalidTypes_DoNotClaim()
        {
            await _model.ClaimAsync((TimedRewardType)99);
            _player.IsLoaded.Returns(false);
            await _model.ClaimAsync(TimedRewardType.Daily);

            _ = _player.DidNotReceiveWithAnyArgs().ClaimRewardAsync(default);
        }

        [Test]
        public void LocaleChange_RefreshesRewardText()
        {
            _localization.TryGet("Menus", "menu.reward.daily", out Arg.Any<ILocalizedString>()).Returns(call =>
            {
                call[2] = new TestLocalizedString("Daily translated");
                return true;
            });

            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("fr");

            Assert.That(_model.Rewards.Value[1].Title, Is.EqualTo("Daily translated"));
        }

        [Test]
        public async Task Disposal_IgnoresLateFailuresAndUnsubscribesFromPlayerChanges()
        {
            var pending = new TaskCompletionSource<ResultType>();
            _player.ClaimRewardAsync(TimedRewardType.Daily).Returns(pending.Task);
            Task claim = _model.ClaimAsync(TimedRewardType.Daily);
            RewardCardData[] before = _model.Rewards.Value;
            _model.Dispose();

            pending.SetResult(Result.Failure("unknown"));
            await claim;
            _data.Apply(default);
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("fr");
            _now = _now.AddDays(1);
            _model.Tick();

            Assert.That(_model.Rewards.Value, Is.SameAs(before));
            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
            _ = _player.DidNotReceive().LoadDataAsync();
        }
    }
}
