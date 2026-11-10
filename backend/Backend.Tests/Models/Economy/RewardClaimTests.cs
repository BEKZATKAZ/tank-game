using Backend.Models.Economy;

namespace Backend.Tests.Models.Economy;

public class RewardClaimTests
{
    [TestCase(CurrencyType.PridePoints, 0)]
    [TestCase(CurrencyType.PridePoints, 200)]
    [TestCase(CurrencyType.PridePoints, 1500)]
    [TestCase(CurrencyType.SpareParts, 0)]
    [TestCase(CurrencyType.SpareParts, 200)]
    [TestCase(CurrencyType.SpareParts, 1500)]
    public void Claim_FixedRange_AddsExactAmount(CurrencyType type, int amount)
    {
        PlayerBalance balance = new();
        balance.Add(type, 100);
        RewardClaim reward = new(type, amount, amount);

        reward.Claim(balance);

        Assert.Multiple(() =>
        {
            Assert.That(type == CurrencyType.SpareParts ? balance.SpareParts : balance.PridePoints,
                Is.EqualTo(100 + amount));

            Assert.That(type == CurrencyType.SpareParts ? balance.PridePoints : balance.SpareParts, Is.Zero);
        });
    }

    [TestCase(200, 400)]
    [TestCase(1000, 1500)]
    public void Claim_RandomRange_AddsAmountWithinInclusiveBounds(int minimum, int maximum)
    {
        PlayerBalance balance = new();
        balance.Add(CurrencyType.PridePoints, 100);
        RewardClaim reward = new(CurrencyType.PridePoints, minimum, maximum);

        reward.Claim(balance);

        Assert.That(balance.PridePoints, Is.InRange(100 + minimum, 100 + maximum));
    }

    [Test]
    public void Claim_RepeatedRewards_Accumulate()
    {
        PlayerBalance balance = new();
        RewardClaim reward = new(CurrencyType.PridePoints, 50, 50);

        reward.Claim(balance);
        reward.Claim(balance);

        Assert.That(balance.PridePoints, Is.EqualTo(100));
    }

    [Test]
    public void Claim_ReversedRange_ThrowsWithoutChangingBalance()
    {
        PlayerBalance balance = new();
        balance.Add(CurrencyType.PridePoints, 100);
        RewardClaim reward = new(CurrencyType.PridePoints, 20, 10);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => reward.Claim(balance));
            Assert.That(balance.PridePoints, Is.EqualTo(100));
        });
    }

    [TestCase(CurrencyType.PridePoints, 0, 100)]
    [TestCase(CurrencyType.PridePoints, 30, 130)]
    [TestCase(CurrencyType.PridePoints, 1000, 1100)]
    [TestCase(CurrencyType.SpareParts, 0, 100)]
    [TestCase(CurrencyType.SpareParts, 30, 130)]
    [TestCase(CurrencyType.SpareParts, 1000, 1100)]
    public void PlayerBalance_Add_ReturnsUpdatedBalance(CurrencyType type, int amount, int expected)
    {
        PlayerBalance balance = new();
        balance.Add(type, 100);

        int result = balance.Add(type, amount);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(type == CurrencyType.SpareParts ? balance.SpareParts : balance.PridePoints,
                Is.EqualTo(expected));

            Assert.That(type == CurrencyType.SpareParts ? balance.PridePoints : balance.SpareParts, Is.Zero);
        });
    }

    [Test]
    public void PlayerBalance_Add_UnsupportedCurrency_PreservesBalance()
    {
        PlayerBalance balance = new();
        balance.Add(CurrencyType.PridePoints, 100);

        int result = balance.Add((CurrencyType)99, 200);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Zero);
            Assert.That(balance.PridePoints, Is.EqualTo(100));
        });
    }
}
