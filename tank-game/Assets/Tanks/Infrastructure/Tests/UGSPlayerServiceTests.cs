using NSubstitute;
using NUnit.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Store;

namespace Tanks.Infrastructure.Tests
{
    public sealed class UGSPlayerServiceTests
    {
        private ICloudCodeCaller _caller;
        private IServerClock _clock;
        private UGSPlayerService _service;
        private PlayerSnapshot _snapshot;

        [SetUp]
        public void Setup()
        {
            _caller = Substitute.For<ICloudCodeCaller>();
            _clock = Substitute.For<IServerClock>();
            _service = new UGSPlayerService(_caller, _clock);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            _snapshot = new PlayerSnapshot
            {
                Level = new PlayerLevel { Value = 7, XP = 450 },
                Balance = new PlayerBalance { SpareParts = 300, PridePoints = 100 },
                Cosmetics = new List<string> { "v4-body" },
                Equipped = new EquippedCosmetics { Body = "v4-body", Turret = "v3-turret" },
                ServerUtc = now,
                Rewards = new RewardDeadlines { Playtime = now.AddMinutes(2), Daily =
                    now.AddMinutes(3) }
            };

            _caller.CallAsync<GetLocalPlayerData,
                PlayerSnapshot>(default).Returns(Result.Success(_snapshot));
        }

        [Test]
        public async Task Load_MapsBackendFieldsBeforePublishingRevision()
        {
            bool published = false;
            _service.LocalPlayer.Revision.Bind(revision =>
            {
                if (revision == 0) return;
                Assert.That(_service.LocalPlayer.Level.Value.Value, Is.EqualTo(7));
                Assert.That(_service.LocalPlayer.Balance.Value.SpareParts, Is.EqualTo(300));
                Assert.That(_service.LocalPlayer.Equipped.Value.Body,
                    Is.EqualTo("v4-body"));

                published = true;
            });

            ResultType result = await _service.LoadDataAsync();

            Assert.True(result.IsSuccess);
            Assert.True(_service.IsLoaded);
            Assert.True(published);
            _clock.Received(1).Synchronize(_snapshot.ServerUtc);
        }

        [Test]
        public async Task Failure_DoesNotReplaceLoadedDataOrSynchronizeClock()
        {
            await _service.LoadDataAsync();
            _clock.ClearReceivedCalls();
            _caller.CallAsync<GetLocalPlayerData,
                PlayerSnapshot>(default).Returns(Result.Failure<PlayerSnapshot>("unknown"));

            ResultType result = await _service.LoadDataAsync();

            Assert.False(result.IsSuccess);
            Assert.That(_service.LocalPlayer.Balance.Value.SpareParts, Is.EqualTo(300));
            _clock.DidNotReceiveWithAnyArgs().Synchronize(default);
        }

        [Test]
        public async Task Mutation_WaitsForLoadAndPublishesLatestSnapshot()
        {
            TaskCompletionSource<ResultType<PlayerSnapshot>> pending = new();
            _caller.CallAsync<GetLocalPlayerData,
                PlayerSnapshot>(default).Returns(pending.Task);

            PlayerSnapshot purchased = _snapshot;
            purchased.Balance.SpareParts = 100;
            purchased.Cosmetics = new List<string> { "v4-body", "v4-turret" };
            _caller.CallAsync<PurchaseCosmetic, PlayerSnapshot>(Arg.Any<PurchaseCosmetic>())
                .Returns(Result.Success(purchased));

            Task<ResultType> loading = _service.LoadDataAsync();
            Task<ResultType> purchase = _service.PurchaseAsync("v4-turret", "parts", 200);
            await _caller.DidNotReceive().CallAsync<PurchaseCosmetic,
                PlayerSnapshot>(Arg.Any<PurchaseCosmetic>());

            pending.SetResult(Result.Success(_snapshot));
            await loading;
            await purchase;

            Assert.That(_service.LocalPlayer.Balance.Value.SpareParts, Is.EqualTo(100));
            Assert.True(_service.LocalPlayer.Owns("v4-turret"));
        }

        [TestCase(TimedRewardType.Daily)]
        [TestCase(TimedRewardType.Playtime)]
        public async Task Claim_UpdatesOnlyBalanceClockAndSelectedDeadline(
            TimedRewardType type)
        {
            await _service.LoadDataAsync();
            _caller.ClearReceivedCalls();
            RewardClaimResult reward = new()
            {
                Balance = new PlayerBalance { SpareParts = 125, PridePoints = 50 },
                ServerUtc = _snapshot.ServerUtc.AddMinutes(1),
                NextClaimAt = _snapshot.ServerUtc.AddMinutes(2)
            };

            _caller.CallAsync<ClaimDailyReward, RewardClaimResult>(default)
                .Returns(Result.Success(reward));

            _caller.CallAsync<ClaimPlaytimeReward, RewardClaimResult>(default)
                .Returns(Result.Success(reward));

            ResultType result = await _service.ClaimRewardAsync(type);
            RewardDeadlines deadlines = _service.LocalPlayer.Rewards.Value;

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_service.LocalPlayer.Balance.Value, Is.EqualTo(reward.Balance));
            Assert.That(_service.LocalPlayer.Equipped.Value,
                Is.EqualTo(_snapshot.Equipped));

            Assert.That(_service.LocalPlayer.Cosmetics.Value,
                Is.EqualTo(_snapshot.Cosmetics));

            Assert.That(type == TimedRewardType.Daily ? deadlines.Daily :
                deadlines.Playtime,
                Is.EqualTo(reward.NextClaimAt));

            Assert.That(type == TimedRewardType.Daily ? deadlines.Playtime :
                deadlines.Daily,
                Is.EqualTo(type == TimedRewardType.Daily
                    ? _snapshot.Rewards.Playtime : _snapshot.Rewards.Daily));

            _clock.Received().Synchronize(reward.ServerUtc);
            _ = _caller.DidNotReceive().CallAsync<GetLocalPlayerData,
                PlayerSnapshot>(default);
        }

        [Test]
        public async Task MissingServerTime_RejectsInvalidSnapshot()
        {
            _snapshot.ServerUtc = default;
            _caller.CallAsync<GetLocalPlayerData,
                PlayerSnapshot>(default).Returns(Result.Success(_snapshot));

            Assert.False((await _service.LoadDataAsync()).IsSuccess);
            Assert.False(_service.IsLoaded);
        }

        [Test]
        public void MissingEquipmentAndInventory_UsesFreeDefaults()
        {
            LocalPlayer player = new();
            player.Apply(default);

            Assert.True(player.Owns("v3-body"));
            Assert.True(player.Owns("v3-turret"));
            Assert.False(player.Owns("v4-body"));
            Assert.That(player.Equipped.Value.Body, Is.EqualTo("v3-body"));
        }
    }
}
