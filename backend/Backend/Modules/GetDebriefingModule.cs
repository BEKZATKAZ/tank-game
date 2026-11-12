using Backend.Models;
using Backend.Models.Debriefing;
using Backend.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public sealed class GetDebriefingModule
{
    public GetDebriefingModule(
        IPlayerDebriefingService players,
        ILogger<GetDebriefingModule>? logger = null)
    {
        _players = players;
        _logger = logger;
    }

    private readonly IPlayerDebriefingService _players;
    private readonly ILogger<GetDebriefingModule>? _logger;

    [CloudCodeFunction("GetDebriefing")]
    public async Task<ApiResult<DebriefingResponse>> Handle(
        IExecutionContext context,
        string matchId,
        PlayerDebriefingPayload[] players)
    {
        string? reason = GetValidationFailure(matchId, players);

        if (reason != null)
        {
            _logger?.LogWarning(
                "Debriefing {MatchId}: invalid payload: {Reason}",
                matchId, reason);

            return ApiResult.Fail<DebriefingResponse>("invalid-payload");
        }

        DebriefingPlayerResult[] results = await Task.WhenAll(
            players.Select(player => CalculatePlayerAsync(context, matchId,
                player)));

        return ApiResult.Ok(new DebriefingResponse(results));
    }

    private async Task<DebriefingPlayerResult> CalculatePlayerAsync(
        IExecutionContext context,
        string matchId,
        PlayerDebriefingPayload player)
    {
        ApiResult<PlayerDebriefing> result = await _players.CalculateAsync(
            context, matchId, player);

        return new DebriefingPlayerResult(
            player.PlayerId, result.Value, result.ErrorCode);
    }

    private static string? GetValidationFailure(
        string matchId,
        PlayerDebriefingPayload[] players)
    {
        if (string.IsNullOrWhiteSpace(matchId)) return "missing-match-id";
        if (players is null) return "missing-players";
        if (players.Length == 0) return "empty-players";

        for (int i = 0; i < players.Length; i++)
        {
            PlayerDebriefingPayload player = players[i];

            if (string.IsNullOrWhiteSpace(player.PlayerId) == false) continue;

            return $"player[{i}]: missing-player-id";
        }

        int distinctPlayers = players
            .Select(player => player.PlayerId)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return distinctPlayers == players.Length ? null : "duplicate-player-id";
    }
}
