using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Models.Economy;
using Backend.Models.Leveling;
using Backend.Services.Adapters;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Services;

public sealed class PlayerDebriefingService : IPlayerDebriefingService
{
    public PlayerDebriefingService(
        ICloudSaveService cloudSave,
        IDebriefingCalculationService calculation,
        ILogger<PlayerDebriefingService>? logger = null)
    {
        _cloudSave = cloudSave;
        _calculation = calculation;
        _logger = logger;
    }

    private readonly ICloudSaveService _cloudSave;
    private readonly ILogger<PlayerDebriefingService>? _logger;
    private readonly IDebriefingCalculationService _calculation;

    public async Task<ApiResult<PlayerDebriefing>> CalculateAsync(
        IExecutionContext context,
        string matchId,
        PlayerDebriefingPayload player)
    {
        try
        {
            ApiResult<CloudItems> loaded = await _cloudSave.LoadItemsAsync(
                context, player.PlayerId, ["level", "balance", "debriefingReceipt"]);

            if (loaded.IsFaulted)
            {
                return ApiResult.Fail<PlayerDebriefing>(loaded.ErrorCode!);
            }

            CloudItems items = loaded.Value!;

            PlayerDebriefing receipt = items.Get("debriefingReceipt",
                new PlayerDebriefing("", "", false, [], default, default,
                    default, default));

            if (receipt.MatchId == matchId) return ApiResult.Ok(receipt);

            Level level = items.Get("level", new Level());
            PlayerBalance balance = items.Get("balance", new PlayerBalance());

            PlayerDebriefing conclusion = _calculation.Calculate(matchId, player,
                level, balance);

            items.Set("debriefingReceipt", conclusion);

            ApiResult saved = await _cloudSave.SaveItemsAsync(context,
                player.PlayerId,
                items);

            return saved.IsFaulted
                ? ApiResult.Fail<PlayerDebriefing>(saved.ErrorCode!)
                : ApiResult.Ok(conclusion);
        }
        catch (OverflowException exception)
        {
            _logger?.LogError(exception,
                "Debriefing {MatchId}: reward overflow for {PlayerId}; " +
                "statistics {Statistics}", matchId, player.PlayerId, player);

            return ApiResult.Fail<PlayerDebriefing>("invalid-payload");
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "Debriefing {MatchId} failed for {PlayerId}",
                matchId, player.PlayerId);

            return ApiResult.Fail<PlayerDebriefing>("unknown");
        }
    }
}
