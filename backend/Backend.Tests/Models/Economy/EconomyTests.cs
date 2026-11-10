using Backend.Models.Economy;
using Newtonsoft.Json;

namespace Backend.Tests.Models.Economy;

public class EconomyTests
{
    [Test]
    public void PlayerBalance_JsonRoundTrip_PreservesPoints()
    {
        PlayerBalance balance = CreateBalance();

        string json = JsonConvert.SerializeObject(balance);
        PlayerBalance restored = JsonConvert.DeserializeObject<PlayerBalance>(json)!;

        Assert.Multiple(() =>
        {
            Assert.That(balance.PridePoints, Is.EqualTo(100));
            Assert.That(restored.PridePoints, Is.EqualTo(100));
            Assert.That(restored.SpareParts, Is.EqualTo(100));
        });
    }

    [TestCase(CurrencyType.SpareParts, 0, true, 100)]
    [TestCase(CurrencyType.SpareParts, 30, true, 70)]
    [TestCase(CurrencyType.SpareParts, 100, true, 0)]
    [TestCase(CurrencyType.SpareParts, 101, false, 100)]
    [TestCase(CurrencyType.SpareParts, -1, false, 100)]
    [TestCase(CurrencyType.PridePoints, 0, true, 100)]
    [TestCase(CurrencyType.PridePoints, 30, true, 70)]
    [TestCase(CurrencyType.PridePoints, 100, true, 0)]
    [TestCase(CurrencyType.PridePoints, 101, false, 100)]
    [TestCase(CurrencyType.PridePoints, -1, false, 100)]
    [TestCase(CurrencyType.PridePoints, int.MinValue, false, 100)]
    [TestCase((CurrencyType)99, 0, false, 100)]
    [TestCase((CurrencyType)99, 30, false, 100)]
    [TestCase((CurrencyType)99, -1, false, 100)]
    public void PlayerBalance_TryConsume_ChangesOnlyValidPayments(
        CurrencyType currency, int amount, bool expected, int remaining)
    {
        PlayerBalance balance = CreateBalance();

        bool result = balance.TryConsume(currency, amount);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(currency == CurrencyType.SpareParts ? balance.SpareParts : balance.PridePoints,
                Is.EqualTo(remaining));

            Assert.That(currency == CurrencyType.SpareParts ? balance.PridePoints : balance.SpareParts,
                Is.EqualTo(100));
        });
    }

    [Test]
    public void PlayerBalance_RepeatedPayments_UseRemainingPoints()
    {
        PlayerBalance balance = CreateBalance();

        Assert.Multiple(() =>
        {
            Assert.That(balance.TryConsume(CurrencyType.PridePoints, 70), Is.True);
            Assert.That(balance.TryConsume(CurrencyType.PridePoints, 40), Is.False);
            Assert.That(balance.PridePoints, Is.EqualTo(30));
        });
    }

    private static PlayerBalance CreateBalance()
    {
        return JsonConvert.DeserializeObject<PlayerBalance>("{\"PridePoints\":100,\"SpareParts\":100}")!;
    }
}
