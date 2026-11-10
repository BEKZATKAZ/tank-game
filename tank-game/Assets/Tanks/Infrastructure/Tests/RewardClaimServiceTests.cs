using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Store;

namespace Tanks.Infrastructure.Tests
{
    public sealed class RewardClaimServiceTests
    {
        [Test]
        public async Task OverlappingClaims_ShareOneBackendRequest()
        {
            ICloudCodeCaller caller = Substitute.For<ICloudCodeCaller>();
            IServerClock clock = Substitute.For<IServerClock>();
            UGSPlayerService player = new(caller, clock);
            TaskCompletionSource<ResultType<RewardClaimResult>> pending = new();
            caller.CallAsync<ClaimDailyReward, RewardClaimResult>(default)
                .Returns(pending.Task);

            Task<ResultType> first = player.ClaimRewardAsync(TimedRewardType.Daily);
            Task<ResultType> second = player.ClaimRewardAsync(TimedRewardType.Daily);
            Assert.That(second, Is.SameAs(first));
            _ = caller.Received(1).CallAsync<ClaimDailyReward, RewardClaimResult>(default);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            pending.SetResult(Result.Success(new RewardClaimResult
            {
                ServerUtc = now,
                NextClaimAt = now.AddMinutes(1)
            }));

            Assert.That((await first).IsSuccess, Is.True);
            _ = caller.DidNotReceive()
                .CallAsync<GetLocalPlayerData, PlayerSnapshot>(default);
        }

        [TestCase("unknown")]
        [TestCase("unauthorized")]
        public async Task FailedClaim_PreservesPlayerDataAndClock(string code)
        {
            ICloudCodeCaller caller = Substitute.For<ICloudCodeCaller>();
            IServerClock clock = Substitute.For<IServerClock>();
            UGSPlayerService player = new(caller, clock);
            PlayerBalance balance = new() { SpareParts = 123, PridePoints = 45 };
            player.LocalPlayer.Balance.Value = balance;
            int revision = player.LocalPlayer.Revision.Value;
            caller.CallAsync<ClaimDailyReward, RewardClaimResult>(default)
                .Returns(Result.Failure<RewardClaimResult>(code));

            ResultType result = await player.ClaimRewardAsync(TimedRewardType.Daily);

            Assert.That(result.ErrorCode, Is.EqualTo(code));
            Assert.That(player.LocalPlayer.Balance.Value, Is.EqualTo(balance));
            Assert.That(player.LocalPlayer.Revision.Value, Is.EqualTo(revision));
            clock.DidNotReceiveWithAnyArgs().Synchronize(default);
        }
    }
}
