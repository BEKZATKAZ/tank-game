namespace Backend.Models.Debriefing;

public readonly record struct DebriefingPlayerResult(
    string PlayerId,
    PlayerDebriefing? Value,
    string? ErrorCode);
