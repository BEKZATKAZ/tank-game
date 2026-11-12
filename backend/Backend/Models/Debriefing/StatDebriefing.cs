namespace Backend.Models.Debriefing;

public readonly record struct StatDebriefing(
    int Value = 0,
    int XP = 0,
    int SpareParts = 0,
    int PridePoints = 0)
{
    public StatDebriefing Calculate(int value)
    {
        return new StatDebriefing(value, checked(XP * value),
            checked(SpareParts * value), checked(PridePoints * value));
    }
}
