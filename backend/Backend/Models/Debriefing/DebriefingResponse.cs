namespace Backend.Models.Debriefing;

public readonly record struct DebriefingResponse(
    DebriefingPlayerResult[] Conclusions);
