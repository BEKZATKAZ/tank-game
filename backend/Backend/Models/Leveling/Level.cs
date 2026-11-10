using Newtonsoft.Json;
using System;

namespace Backend.Models.Leveling;

public sealed class Level
{
    private const int BASE_XP = 500;
    private const float MULTIPLIER = 1.06f;

    [JsonIgnore]
    public int RequiredXP
    {
        get
        {
            int required = (int)(BASE_XP * MathF.Pow(MULTIPLIER, Value - 1));
            return (int)(required * 0.1f) * 10;
        }
    }

    [JsonProperty]
    public int Value { get; private set; } = 1;

    [JsonProperty]
    public int XP { get; private set; }

    public void AddXP(int xp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(xp, nameof(xp));
        long remaining = (long)XP + xp;

        while (remaining >= RequiredXP)
        {
            remaining -= RequiredXP;
            Value++;
        }

        XP = (int)remaining;
    }
}
