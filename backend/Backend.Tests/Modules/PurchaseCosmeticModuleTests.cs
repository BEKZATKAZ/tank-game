using Backend.Models;
using Backend.Models.Economy;
using Backend.Models.Store;
using Backend.Modules;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Modules;

public class PurchaseCosmeticModuleTests
{
    private IExecutionContext _context = null!;
    private IRemoteConfigService _remoteConfig = null!;
    private ICloudSaveService _cloudSave = null!;
    private PurchaseCosmeticModule _module = null!;
    private CloudItems _player = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _remoteConfig = Substitute.For<IRemoteConfigService>();
        _cloudSave = Substitute.For<ICloudSaveService>();

        _module = new PurchaseCosmeticModule(_remoteConfig, _cloudSave);
        _player = CreatePlayer();

        SetPrice(30);
        _remoteConfig.GetAsync<DiscountDefinition>(_context, "store.discounted-items", "new")
            .Returns(ApiResult.Fail<DiscountDefinition>("not-found"));

        _cloudSave.LoadItemsAsync(_context, Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(_player));

        _cloudSave.SaveItemsAsync(_context, Arg.Any<CloudItems>())
            .Returns(ApiResult.Ok());
    }

    [TestCase(0, 100)]
    [TestCase(30, 70)]
    [TestCase(100, 0)]
    public async Task Handle_ValidPurchase_DeductsPriceAndSavesOnce(int price, int remaining)
    {
        SetPrice(price);

        ApiResult result = await Handle();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_player.Get<List<string>>("cosmetics"),
                Is.EqualTo(new[] { "existing", "new" }));

            Assert.That(_player.Get<PlayerBalance>("balance").PridePoints, Is.EqualTo(remaining));
        });

        await _remoteConfig.Received(1).GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", "new");

        await _cloudSave.Received(1).LoadItemsAsync(_context,
            Arg.Is<List<string>>(keys => keys.SequenceEqual(new[] { "cosmetics", "balance" })));

        await _cloudSave.Received(1).SaveItemsAsync(_context, _player);

        Assert.Multiple(() =>
        {
            Assert.That(_player.GetSetters().Single(x => x.Key == "cosmetics").WriteLock,
                Is.EqualTo("cosmetics-lock"));

            Assert.That(_player.GetSetters().Single(x => x.Key == "balance").WriteLock,
                Is.EqualTo("balance-lock"));
        });
    }

    [Test]
    public async Task Handle_ConfigFailure_SkipsLoadAndSave()
    {
        _remoteConfig.GetAsync<CosmeticDefinition>(_context, "store.cosmetics", "new")
            .Returns(ApiResult.Fail<CosmeticDefinition>("not-found"));

        Assert.That(await Handle(), Is.EqualTo(ApiResult.Fail("not-found")));

        await _cloudSave.DidNotReceive().LoadItemsAsync(_context, Arg.Any<List<string>>());
        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [Test]
    public async Task Handle_LoadFailure_SkipsSave()
    {
        _cloudSave.LoadItemsAsync(_context, Arg.Any<List<string>>())
            .Returns(ApiResult.Fail<CloudItems>("unauthorized"));

        Assert.That(await Handle(), Is.EqualTo(ApiResult.Fail("unauthorized")));

        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [Test]
    public async Task Handle_AlreadyUnlocked_PreservesBalanceAndSkipsSave()
    {
        _player.Get<List<string>>("cosmetics").Add("new");

        ApiResult result = await Handle();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ApiResult.Fail("already-unlocked")));
            Assert.That(_player.Get<PlayerBalance>("balance").PridePoints, Is.EqualTo(100));
            Assert.That(_player.Get<List<string>>("cosmetics"), Has.Count.EqualTo(2));
        });

        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase(CurrencyType.PridePoints, 101)]
    [TestCase(CurrencyType.PridePoints, -1)]
    [TestCase(CurrencyType.SpareParts, 101)]
    [TestCase(CurrencyType.SpareParts, -1)]
    public async Task Handle_InvalidPayment_PreservesPlayerAndSkipsSave(
        CurrencyType currency, int amount)
    {
        SetPrice(amount, currency);

        string currencyName = currency == CurrencyType.SpareParts ? "spareParts" : "pridePoints";
        ApiResult result = await _module.Handle(_context, "new", currencyName);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(ApiResult.Fail("insufficient-funds")));
            Assert.That(_player.Get<PlayerBalance>("balance").PridePoints, Is.EqualTo(100));
            Assert.That(_player.Get<List<string>>("cosmetics"), Is.EqualTo(new[] { "existing" }));
        });

        await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [Test]
    public async Task Handle_SaveFailure_PropagatesErrorWithoutRetry()
    {
        _cloudSave.SaveItemsAsync(_context, _player).Returns(ApiResult.Fail("conflict"));

        Assert.That(await Handle(), Is.EqualTo(ApiResult.Fail("conflict")));

        await _cloudSave.Received(1).SaveItemsAsync(_context, _player);
    }

    [Test]
    public async Task Handle_SecondPurchase_DoesNotChargeOrSaveAgain()
    {
        Assert.That((await Handle()).IsSuccess, Is.True);

        ApiResult second = await Handle();

        Assert.Multiple(() =>
        {
            Assert.That(second.ErrorCode, Is.EqualTo("already-unlocked"));
            Assert.That(_player.Get<PlayerBalance>("balance").PridePoints, Is.EqualTo(70));
            Assert.That(_player.Get<List<string>>("cosmetics"), Is.EqualTo(new[] { "existing", "new" }));
        });

        await _cloudSave.Received(1).SaveItemsAsync(_context, _player);
    }

    [TestCase(true, false, 30, true)]
    [TestCase(false, true, 0, true)]
    [TestCase(true, true, 0, true)]
    [TestCase(false, true, 30, false)]
    public async Task Handle_MissingKeys_UsesEmptyCosmeticsAndZeroBalance(
        bool missingCosmetics, bool missingBalance, int price, bool success)
    {
        List<Item> items = [];
        if (missingCosmetics == false)
        {
            items.Add(CloudSaveItems.Create("cosmetics", new JArray("existing"), "cosmetics-lock"));
        }

        if (missingBalance == false)
        {
            items.Add(CloudSaveItems.Create("balance", JObject.Parse("{\"PridePoints\":100}"), "balance-lock"));
        }

        _player = new CloudItems(items);
        SetPrice(price);
        _cloudSave.LoadItemsAsync(_context, Arg.Any<List<string>>())
            .Returns(ApiResult.Ok(_player));

        ApiResult result = await Handle();

        Assert.That(result.IsSuccess, Is.EqualTo(success));
        if (success == false)
        {
            Assert.That(result.ErrorCode, Is.EqualTo("insufficient-funds"));
            await _cloudSave.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(_player.Get<List<string>>("cosmetics"), Does.Contain("new"));
            Assert.That(_player.Get<PlayerBalance>("balance").PridePoints,
                Is.EqualTo(missingBalance ? 0 : 100 - price));

            Assert.That(_player.GetSetters().Single(x => x.Key == "cosmetics").WriteLock,
                Is.EqualTo(missingCosmetics ? null : "cosmetics-lock"));

            Assert.That(_player.GetSetters().Single(x => x.Key == "balance").WriteLock,
                Is.EqualTo(missingBalance ? null : "balance-lock"));
        });

        await _cloudSave.Received(1).SaveItemsAsync(_context, _player);
    }

    private Task<ApiResult> Handle()
    {
        return _module.Handle(_context, "new", "pridePoints");
    }

    private void SetPrice(int amount, CurrencyType currency = CurrencyType.PridePoints)
    {
        string currencyName = currency == CurrencyType.SpareParts ? "spareParts" : "pridePoints";

        _remoteConfig.GetAsync<CosmeticDefinition>(_context, "store.cosmetics", "new")
            .Returns(ApiResult.Ok(new CosmeticDefinition("body",
                new UnlockConditions(new Dictionary<string, int> { [currencyName] = amount }))));
    }

    private static CloudItems CreatePlayer()
    {
        return new CloudItems([
            CloudSaveItems.Create("cosmetics", new JArray("existing"), "cosmetics-lock"),
            CloudSaveItems.Create("balance", JObject.Parse("{\"PridePoints\":100}"), "balance-lock")
        ]);
    }
}
