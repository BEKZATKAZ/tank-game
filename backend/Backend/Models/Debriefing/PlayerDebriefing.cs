namespace Backend.Models.Debriefing;

public sealed record PlayerDebriefing(
    string MatchId,
    string PlayerId,
    bool IsVictory,
    StatDebriefing[] Rows,
    DebriefingLevel BeforeLevel,
    DebriefingLevel AfterLevel,
    DebriefingBalance BeforeBalance,
    DebriefingBalance AfterBalance);
