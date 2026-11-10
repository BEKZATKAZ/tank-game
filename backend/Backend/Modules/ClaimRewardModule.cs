using Backend.Models;
using Backend.Models.Economy;
using Backend.Services;
using Backend.Services.Adapters;
using System;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public sealed class ClaimRewardModule
{
    public ClaimRewardModule(ICloudSaveService cloudSave)
    {
        _cloudSave = cloudSave;
    }

    private readonly ICloudSaveService _cloudSave;
    private readonly RewardDefinitionService _rewards = new();

    [CloudCodeFunction("ClaimPlaytimeReward")]
    public Task<ApiResult<RewardClaimResult>> ClaimPlaytime(IExecutionContext context)
    {
        return ClaimAsync(context, TimedRewardType.Playtime);
    }

    [CloudCodeFunction("ClaimDailyReward")]
    public Task<ApiResult<RewardClaimResult>> ClaimDaily(IExecutionContext context)
    {
        return ClaimAsync(context, TimedRewardType.Daily);
    }

    private async Task<ApiResult<RewardClaimResult>> ClaimAsync(
        IExecutionContext context,
        TimedRewardType type)
    {
        ApiResult<CloudItems> loaded = await _cloudSave.LoadItemsAsync(
            context, ["lastClaimedTimes", "balance"]);

        if (loaded.IsFaulted) return ApiResult.Fail<RewardClaimResult>(loaded.ErrorCode!);

        CloudItems player = loaded.Value!;
        PlayerBalance balance = player.Get("balance", new PlayerBalance());
        LastClaimedTimes times = player.Get("lastClaimedTimes", new LastClaimedTimes());
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (times.TryClaim(type, balance, _rewards.Get(type), now) == false)
        {
            return ApiResult.Fail<RewardClaimResult>("already-claimed");
        }

        ApiResult saved = await _cloudSave.SaveItemsAsync(context, player);
        if (saved.IsFaulted) return ApiResult.Fail<RewardClaimResult>(saved.ErrorCode!);

        return ApiResult.Ok(new RewardClaimResult(
            balance, DateTimeOffset.UtcNow, times.GetDeadline(type)));
    }
}
