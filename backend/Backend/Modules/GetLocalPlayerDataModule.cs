using Backend.Models;
using Backend.Models.Economy;
using Backend.Models.Leveling;
using Backend.Models.Store;
using Backend.Services.Adapters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public readonly record struct RewardDeadlines(
    DateTimeOffset Playtime, DateTimeOffset Daily);

public readonly record struct PlayerDTO(
    Level Level, PlayerBalance Balance, List<string> Cosmetics,
    EquippedCosmetics Equipped, DateTimeOffset ServerUtc, RewardDeadlines Rewards);

public sealed class GetLocalPlayerDataModule(ICloudSaveService cloudSave)
{
    [CloudCodeFunction("GetLocalPlayerData")]
    public async Task<ApiResult<PlayerDTO>> Handle(IExecutionContext context)
    {
        ApiResult<CloudItems> result = await cloudSave.LoadItemsAsync(context,
            ["level", "balance", "cosmetics", "equippedCosmetics", "lastClaimedTimes"]);

        if (result.IsFaulted) return ApiResult.Fail<PlayerDTO>(result.ErrorCode!);

        CloudItems player = result.Value!;

        var level = player.Get("level", fallback: new Level());
        var balance = player.Get("balance", fallback: new PlayerBalance());
        var cosmetics = player.Get("cosmetics", fallback: new List<string>());
        var equipped = player.Get("equippedCosmetics", fallback: EquippedCosmetics.Default);
        var times = player.Get("lastClaimedTimes", fallback: new LastClaimedTimes());

        string body = cosmetics.Contains(equipped.Body) ? equipped.Body : "v3-body";
        string turret = cosmetics.Contains(equipped.Turret) ? equipped.Turret : "v3-turret";

        DateTimeOffset now = DateTimeOffset.UtcNow;

        RewardDeadlines deadlines = new RewardDeadlines(
            times.GetDeadline(TimedRewardType.Playtime),
            times.GetDeadline(TimedRewardType.Daily));

        return ApiResult.Ok(new PlayerDTO(
            level, balance, cosmetics,
            new EquippedCosmetics(body, turret), now, deadlines));
    }
}
