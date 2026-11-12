using Backend.Models.Debriefing;
using Backend.Models.Economy;
using Backend.Models.Leveling;
using System;

namespace Backend.Services;

public sealed class DebriefingCalculationService : IDebriefingCalculationService
{
    public static readonly StatDebriefing EliminationReward = new(XP: 20, SpareParts: 100);
    public static readonly StatDebriefing AssistReward = new(XP: 10, SpareParts: 10);
    public static readonly StatDebriefing DeathPenalty = new(PridePoints: -5);
    public static readonly StatDebriefing ScoreReward = new(XP: 100, PridePoints: 20);
    public static readonly StatDebriefing VictoryReward = new(XP: 500, PridePoints: 200);

    public PlayerDebriefing Calculate(
        string matchId,
        PlayerDebriefingPayload player,
        Level level,
        PlayerBalance balance)
    {
        if (level.Value < 1 || level.RequiredXP <= 0 || level.XP < 0 ||
            level.XP >= level.RequiredXP || balance.SpareParts < 0 || balance.PridePoints < 0)
        {
            throw new InvalidOperationException("Invalid stored progression");
        }

        DebriefingLevel beforeLevel = new(level.Value, level.XP);
        DebriefingBalance beforeBalance = new(balance.SpareParts, balance.PridePoints);

        StatDebriefing[] rows =
        [
            EliminationReward.Calculate(Math.Max(player.Eliminations, 0)),
            AssistReward.Calculate(Math.Max(player.Assists, 0)),
            DeathPenalty.Calculate(Math.Max(player.Deaths, 0)),
            ScoreReward.Calculate(Math.Max(player.Score, 0)),
            VictoryReward.Calculate(player.IsVictory ? 1 : 0)
        ];

        int totalXP = 0;
        int parts = balance.SpareParts;
        int pride = balance.PridePoints;

        for (int i = 0; i < rows.Length; i++)
        {
            StatDebriefing row = rows[i];

            int appliedPride = Math.Max(row.PridePoints, -pride);
            totalXP = checked(totalXP + row.XP);
            parts = checked(parts + row.SpareParts);
            pride = checked(pride + appliedPride);
            rows[i] = row with { PridePoints = appliedPride };
        }

        level.AddXP(totalXP);
        balance.Add(CurrencyType.SpareParts, parts - balance.SpareParts);
        balance.Add(CurrencyType.PridePoints, pride - balance.PridePoints);

        return new PlayerDebriefing(matchId, player.PlayerId, player.IsVictory, rows,
            beforeLevel, new(level.Value, level.XP), beforeBalance, new(parts, pride));
    }
}
