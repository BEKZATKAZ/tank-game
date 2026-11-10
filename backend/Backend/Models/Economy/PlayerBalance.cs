using Newtonsoft.Json;

namespace Backend.Models.Economy;

public sealed class PlayerBalance
{

    [JsonProperty]
    public int SpareParts { get; private set; }

    [JsonProperty]
    public int PridePoints { get; private set; }

    public int Add(CurrencyType type, int amount)
    {
        return type switch
        {
            CurrencyType.SpareParts => SpareParts += amount,
            CurrencyType.PridePoints => PridePoints += amount,
            _ => 0
        };
    }

    public bool TryConsume(CurrencyType type, int amount)
    {
        if (amount < 0 || type is not (CurrencyType.SpareParts or CurrencyType.PridePoints)) return false;

        int points = type switch
        {
            CurrencyType.SpareParts => SpareParts,
            CurrencyType.PridePoints => PridePoints,
            _ => 0
        };

        if (points < amount) return false;

        return (type switch
        {
            CurrencyType.SpareParts => SpareParts -= amount,
            CurrencyType.PridePoints => PridePoints -= amount,
            _ => 0
        }) >= 0;
    }
}
