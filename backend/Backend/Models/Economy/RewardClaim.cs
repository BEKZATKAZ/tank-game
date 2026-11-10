using System.Security.Cryptography;

namespace Backend.Models.Economy;

public record RewardClaim(CurrencyType CurrencyType, int MinValue, int MaxValue)
{
    public void Claim(PlayerBalance balance)
    {
        int amount = RandomNumberGenerator.GetInt32(MinValue, MaxValue + 1);
        balance.Add(CurrencyType, amount);
    }
}
