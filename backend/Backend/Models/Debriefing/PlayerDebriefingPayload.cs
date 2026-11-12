namespace Backend.Models.Debriefing;

public readonly record struct PlayerDebriefingPayload(
    string PlayerId,
    int Eliminations,
    int Assists,
    int Deaths,
    int Score,
    bool IsVictory);
