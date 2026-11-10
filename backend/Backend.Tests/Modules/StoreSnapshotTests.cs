using Backend.Models;
using Backend.Models.Economy;
using Backend.Models.Store;
using Backend.Modules;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using NSubstitute;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Modules;

public class StoreSnapshotTests
{
    private IExecutionContext _context = null!;
    private ICloudSaveService _save = null!;
    private IRemoteConfigService _config = null!;
    private Dictionary<string, Item> _items = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _save = Substitute.For<ICloudSaveService>();
        _config = Substitute.For<IRemoteConfigService>();
        _items = new();
        Set("balance", new { SpareParts = 5000, PridePoints = 1000 });
        Set("cosmetics", new[] { "v4-body", "v4-turret" });

        _save.LoadItemsAsync(_context, Arg.Any<List<string>>()).Returns(call =>
            ApiResult.Ok(new CloudItems(call.Arg<List<string>>()
                .Where(_items.ContainsKey).Select(key => _items[key]).ToList())));

        _save.SaveItemsAsync(_context, Arg.Any<CloudItems>()).Returns(call =>
        {
            foreach (SetItemBody setter in call.Arg<CloudItems>().GetSetters())
                Set(setter.Key, setter.Value);

            return ApiResult.Ok();
        });

        _config.GetAsync<CosmeticDefinition>(_context, "store.cosmetics", Arg.Any<string>())
            .Returns(ApiResult.Ok(new CosmeticDefinition(null!, new UnlockConditions(
                new Dictionary<string, int> { ["parts"] = 101, ["pride"] = 101 }))));

        _config.GetAsync<DiscountDefinition>(_context, "store.discounted-items",
            Arg.Any<string>())
            .Returns(ApiResult.Ok(new DiscountDefinition(25, null)));
    }

    [Test]
    public async Task Equip_OwnedSlotsPersistIndependentlyAndReturnSnapshot()
    {
        EquipCosmeticModule module = new(_save);
        ApiResult<PlayerDTO> body = await module.Handle(_context, "v4-body");
        ApiResult<PlayerDTO> turret = await module.Handle(_context, "v4-turret");

        Assert.Multiple(() =>
        {
            Assert.That(body.Value.Equipped, Is.EqualTo(new EquippedCosmetics("v4-body",
                "v3-turret")));

            Assert.That(turret.Value.Equipped, Is.EqualTo(new
                EquippedCosmetics("v4-body", "v4-turret")));

            Assert.That(turret.Value.Balance.SpareParts, Is.EqualTo(5000));
            Assert.That(turret.Value.ServerUtc,
                Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1)));
        });
    }

    [TestCase("v5-body", "not-unlocked")]
    [TestCase("v4-wing", "invalid-data")]
    [TestCase("", "invalid-data")]
    public async Task Equip_InvalidOrUnowned_DoesNotSave(string id, string error)
    {
        ApiResult<PlayerDTO> result = await new
            EquipCosmeticModule(_save).Handle(_context, id);

        Assert.That(result.ErrorCode, Is.EqualTo(error));
        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase("v3-body")]
    [TestCase("v3-turret")]
    public async Task Equip_DefaultIsFreeWithoutCatalog(string id)
    {
        ApiResult<PlayerDTO> result = await new
            EquipCosmeticModule(_save).Handle(_context, id);

        Assert.That(result.IsSuccess, Is.True);
        await _config.DidNotReceive().GetAsync<CosmeticDefinition>(_context,
            Arg.Any<string>(), Arg.Any<string>());
    }

    [TestCase("parts", 4924, 1000)]
    [TestCase("pride", 5000, 924)]
    public async Task Purchase_PermanentDiscountAndNewCurrencyKeys_ReturnUpdatedSnapshot(
        string currency,
        int parts,
        int pride)
    {
        ApiResult<PlayerDTO> result = await new PurchaseCosmeticModule(_config,
            _save).Purchase(_context, "v2-body", currency);

        string json = JsonConvert.SerializeObject(result);
        ApiResult<PlayerDTO> restored = JsonConvert
            .DeserializeObject<ApiResult<PlayerDTO>>(json);

        Assert.Multiple(() =>
        {
            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Value.Balance.SpareParts, Is.EqualTo(parts));
            Assert.That(restored.Value.Balance.PridePoints, Is.EqualTo(pride));
            Assert.That(restored.Value.Cosmetics, Does.Contain("v2-body"));
            Assert.That(restored.Value.Equipped, Is.EqualTo(EquippedCosmetics.Default));
        });
    }

    [Test]
    public async Task Claim_ReturnsUpdatedBalanceAndDeadline()
    {
        ApiResult<RewardClaimResult> result = await new
            ClaimRewardModule(_save).ClaimPlaytime(_context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Balance.SpareParts, Is.GreaterThan(5000));
            Assert.That(result.Value.NextClaimAt, Is.GreaterThan(result.Value.ServerUtc));
        });

        ApiResult<RewardClaimResult> again = await new
            ClaimRewardModule(_save).ClaimPlaytime(_context);

        Assert.That(again.ErrorCode, Is.EqualTo("already-claimed"));
    }

    [TestCase(TimedRewardType.Playtime)]
    [TestCase(TimedRewardType.Daily)]
    public void Reward_ExactDeadlineIsClaimable(TimedRewardType type)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        LastClaimedTimes times = JsonConvert.DeserializeObject<LastClaimedTimes>(
            JsonConvert.SerializeObject(new { Playtime = now, Daily = now }))!;

        DateTimeOffset deadline = times.GetDeadline(type);

        Assert.That(times.TryClaim(type, new PlayerBalance(), [],
            deadline.AddTicks(-1)), Is.False);

        Assert.That(times.TryClaim(type, new PlayerBalance(), [], deadline), Is.True);
    }

    [Test]
    public async Task Purchase_ChangedQuote_DoesNotSpendOrUnlock()
    {
        ApiResult<PlayerDTO> result = await new PurchaseCosmeticModule(_config,
            _save).Purchase(_context, "v2-body", "parts", 75);

        Assert.That(result.ErrorCode, Is.EqualTo("price-changed"));
        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [Test]
    public async Task Equip_OwnedCosmetic_DoesNotRequireShoppingCatalog()
    {
        _config.GetAsync<CosmeticDefinition>(_context, Arg.Any<string>(), Arg.Any<string>())
            .Returns(ApiResult.Fail<CosmeticDefinition>("unknown"));

        ApiResult<PlayerDTO> result = await new
            EquipCosmeticModule(_save).Handle(_context, "v4-body");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Equipped.Body, Is.EqualTo("v4-body"));
    }

    private void Set(string key, object value)
    {
        _items[key] = CloudSaveItems.Create(key, JToken.FromObject(value), key + "-lock");
    }
}
