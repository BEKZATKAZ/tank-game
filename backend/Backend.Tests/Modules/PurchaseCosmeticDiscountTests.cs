using Backend.Models;
using Backend.Models.Economy;
using Backend.Models.Store;
using Backend.Modules;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Modules;

public class PurchaseCosmeticDiscountTests
{
    private IExecutionContext _context = null!;
    private IRemoteConfigService _config = null!;
    private ICloudSaveService _save = null!;
    private PurchaseCosmeticModule _module = null!;
    private CloudItems _player = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _config = Substitute.For<IRemoteConfigService>();
        _save = Substitute.For<ICloudSaveService>();
        _module = new PurchaseCosmeticModule(_config, _save);
        _player = new CloudItems([
            CloudSaveItems.Create("balance",
                JObject.FromObject(new { PridePoints = int.MaxValue, SpareParts = int.MaxValue }), "balance-lock"),
            CloudSaveItems.Create("cosmetics", new JArray("existing"), "cosmetics-lock")
        ]);

        SetPrices(new Dictionary<string, int> { ["pridePoints"] = 101, ["spareParts"] = 101 });
        _config.GetAsync<DiscountDefinition>(_context, "store.discounted-items", Arg.Any<string>())
            .Returns(ApiResult.Fail<DiscountDefinition>("not-found"));

        _save.LoadItemsAsync(_context, Arg.Any<List<string>>()).Returns(ApiResult.Ok(_player));
        _save.SaveItemsAsync(_context, _player).Returns(ApiResult.Ok());
    }

    [TestCase("pridePoints", 101, 25, 76)]
    [TestCase("spareParts", 101, 25, 76)]
    [TestCase("pridePoints", 100, 25, 75)]
    [TestCase("spareParts", 100, 25, 75)]
    [TestCase("pridePoints", 101, 0, 101)]
    [TestCase("spareParts", 101, 0, 101)]
    [TestCase("pridePoints", 101, 100, 0)]
    [TestCase("spareParts", 101, 100, 0)]
    [TestCase("pridePoints", 1, 99, 1)]
    [TestCase("spareParts", 0, 25, 0)]
    [TestCase("pridePoints", int.MaxValue, 25, 1610612736)]
    [TestCase("spareParts", int.MaxValue, 0, int.MaxValue)]
    [TestCase("pridePoints", int.MaxValue, 99, 21474837)]
    public async Task Handle_ActiveDiscount_RoundsUpWithoutOverflow(
        string currency, int price, int percent, int expectedPrice)
    {
        SetPrices(new Dictionary<string, int> { [currency] = price });
        SetDiscount(percent, DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(5)));

        ApiResult result = await _module.Handle(_context, "new", currency);

        AssertPurchased(result, currency, expectedPrice);
        await _config.Received(1).GetAsync<DiscountDefinition>(_context, "store.discounted-items", "new");
        await _save.Received(1).SaveItemsAsync(_context, _player);
    }

    [TestCase("pridePoints", 25, -1)]
    [TestCase("spareParts", 25, -1)]
    [TestCase("pridePoints", 25, 0)]
    [TestCase("spareParts", 25, 0)]
    [TestCase("pridePoints", -1, 1)]
    [TestCase("spareParts", 101, 1)]
    [TestCase("pridePoints", int.MinValue, 1)]
    [TestCase("spareParts", int.MaxValue, 1)]
    public async Task Handle_ExpiredOrInvalidDiscount_PreservesOriginalPrice(
        string currency, int percent, int daysUntilExpiry)
    {
        SetDiscount(percent, DateTimeOffset.UtcNow.AddDays(daysUntilExpiry));

        ApiResult result = await _module.Handle(_context, "new", currency);

        AssertPurchased(result, currency, 101);
    }

    [TestCase("pridePoints")]
    [TestCase("spareParts")]
    public async Task Handle_NoMatchingDiscount_PreservesOriginalPrice(string currency)
    {
        _config.GetAsync<DiscountDefinition>(_context, "store.discounted-items", "other")
            .Returns(ApiResult.Ok(new DiscountDefinition(100, DateTimeOffset.UtcNow.AddDays(1))));

        ApiResult result = await _module.Handle(_context, "new", currency);

        AssertPurchased(result, currency, 101);
        await _config.DidNotReceive().GetAsync<DiscountDefinition>(_context, "store.discounted-items", "other");
    }

    [TestCase("unknown")]
    [TestCase("invalid-data")]
    [TestCase("unauthorized")]
    public async Task Handle_DiscountLookupFailure_PropagatesErrorWithoutMutation(string error)
    {
        _config.GetAsync<DiscountDefinition>(_context, "store.discounted-items", "new")
            .Returns(ApiResult.Fail<DiscountDefinition>(error));

        ApiResult result = await _module.Handle(_context, "new", "pridePoints");

        AssertRejected(result, error);
        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase("pridePoints")]
    [TestCase("spareParts")]
    public async Task Handle_DiscountMakesPurchaseAffordable_UsesReducedPrice(string currency)
    {
        PlayerBalance balance = _player.Get<PlayerBalance>("balance");
        CurrencyType type = currency == "pridePoints" ? CurrencyType.PridePoints : CurrencyType.SpareParts;
        balance.TryConsume(type, int.MaxValue - 100);
        SetPrices(new Dictionary<string, int> { [currency] = 150 });
        SetDiscount(50, DateTimeOffset.UtcNow.AddDays(1));

        ApiResult result = await _module.Handle(_context, "new", currency);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(GetBalance(currency), Is.EqualTo(25));
            Assert.That(_player.Get<List<string>>("cosmetics"), Does.Contain("new"));
        });
    }

    [TestCase("pridePoints", -1)]
    [TestCase("spareParts", int.MinValue)]
    public async Task Handle_NegativeBasePrice_RejectsEvenWithFullDiscount(string currency, int price)
    {
        SetPrices(new Dictionary<string, int> { [currency] = price });
        SetDiscount(100, DateTimeOffset.UtcNow.AddDays(1));

        ApiResult result = await _module.Handle(_context, "new", currency);

        AssertRejected(result, "insufficient-funds");
        await _config.DidNotReceive().GetAsync<DiscountDefinition>(
            _context, "store.discounted-items", Arg.Any<string>());

        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase("null-prices", "pridePoints", "unpurchasable-item")]
    [TestCase("missing-currency", "pridePoints", "invalid-data")]
    [TestCase("unknown-currency", "unknown", "invalid-data")]
    public async Task Handle_InvalidPriceDefinition_RejectsPurchase(
        string scenario, string currency, string error)
    {
        Dictionary<string, int>? prices = scenario switch
        {
            "null-prices" => null,
            "missing-currency" => new Dictionary<string, int> { ["spareParts"] = 30 },
            _ => new Dictionary<string, int> { ["unknown"] = 0 }
        };

        SetPrices(prices);

        ApiResult result = await _module.Handle(_context, "new", currency);

        AssertRejected(result, error);
        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    private void SetPrices(Dictionary<string, int>? prices)
    {
        _config.GetAsync<CosmeticDefinition>(_context, "store.cosmetics", "new")
            .Returns(ApiResult.Ok(new CosmeticDefinition("body", new UnlockConditions(prices))));
    }

    private void SetDiscount(int percent, DateTimeOffset endDate)
    {
        _config.GetAsync<DiscountDefinition>(_context, "store.discounted-items", "new")
            .Returns(ApiResult.Ok(new DiscountDefinition(percent, endDate)));
    }

    private int GetBalance(string currency)
    {
        PlayerBalance balance = _player.Get<PlayerBalance>("balance");
        return currency == "spareParts" ? balance.SpareParts : balance.PridePoints;
    }

    private void AssertPurchased(ApiResult result, string currency, int expectedPrice)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(GetBalance(currency), Is.EqualTo(int.MaxValue - expectedPrice));
            Assert.That(GetBalance(currency == "pridePoints" ? "spareParts" : "pridePoints"), Is.EqualTo(int.MaxValue));
            Assert.That(_player.Get<List<string>>("cosmetics"), Is.EqualTo(new[] { "existing", "new" }));
        });
    }

    private void AssertRejected(ApiResult result, string error)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ApiResult.Fail(error)));
            Assert.That(GetBalance("pridePoints"), Is.EqualTo(int.MaxValue));
            Assert.That(GetBalance("spareParts"), Is.EqualTo(int.MaxValue));
            Assert.That(_player.Get<List<string>>("cosmetics"), Is.EqualTo(new[] { "existing" }));
        });
    }
}
