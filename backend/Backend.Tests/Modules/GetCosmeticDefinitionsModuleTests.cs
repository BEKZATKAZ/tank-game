using Backend.Models;
using Backend.Models.Store;
using Backend.Modules;
using Backend.Services.Adapters;
using Newtonsoft.Json;
using NSubstitute;
using Unity.Services.CloudCode.Core;

namespace Backend.Tests.Modules;

public class GetCosmeticDefinitionsModuleTests
{
    private IExecutionContext _context = null!;
    private IRemoteConfigService _config = null!;
    private GetCosmeticDefinitionsModule _module = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _config = Substitute.For<IRemoteConfigService>();
        _module = new GetCosmeticDefinitionsModule(_config);
    }

    [Test]
    public async Task Handle_Success_ReturnsBothDictionariesKeyedByItemId()
    {
        Dictionary<string, CosmeticDefinition> items = new()
        {
            ["body-1"] = new CosmeticDefinition("body",
                new UnlockConditions(new Dictionary<string, int> { ["spareParts"] = 100 }))
        };

        Dictionary<string, DiscountDefinition> discounts = new()
        {
            ["body-1"] = new DiscountDefinition(25, DateTimeOffset.UtcNow.AddDays(1))
        };

        SetResults(ApiResult.Ok(items), ApiResult.Ok(discounts));

        ApiResult<CosmeticDefinitionsResponse> result = await _module.Handle(_context);

        string json = JsonConvert.SerializeObject(result);
        ApiResult<CosmeticDefinitionsResponse> restored = JsonConvert
            .DeserializeObject<ApiResult<CosmeticDefinitionsResponse>>(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Items, Is.SameAs(items));
            Assert.That(result.Value.Discounts, Is.SameAs(discounts));
            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Value.Items["body-1"].Conditions.Prices!["spareParts"], Is.EqualTo(100));
            Assert.That(restored.Value.Discounts["body-1"], Is.EqualTo(discounts["body-1"]));
        });

        await _config.Received(1).GetAsync<Dictionary<string, CosmeticDefinition>>(_context, "store.cosmetics");
        await _config.Received(1).GetAsync<Dictionary<string, DiscountDefinition>>(_context, "store.discounted-items");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Handle_EmptyOrNullSuccessfulValues_ReturnsEmptyCollections(bool nullValues)
    {
        SetResults(
            ApiResult.Ok<Dictionary<string, CosmeticDefinition>>(nullValues ? null! : []),
            ApiResult.Ok<Dictionary<string, DiscountDefinition>>(nullValues ? null! : []));

        ApiResult<CosmeticDefinitionsResponse> result = await _module.Handle(_context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Items, Is.Empty);
            Assert.That(result.Value.Discounts, Is.Empty);
        });
    }

    [TestCase("not-found")]
    [TestCase("unknown")]
    public async Task Handle_ItemLookupFailure_PropagatesErrorAndSkipsDiscountLookup(string error)
    {
        SetResults(ApiResult.Fail<Dictionary<string, CosmeticDefinition>>(error),
            ApiResult.Ok(new Dictionary<string, DiscountDefinition>()));

        ApiResult<CosmeticDefinitionsResponse> result = await _module.Handle(_context);

        Assert.That(result, Is.EqualTo(ApiResult.Fail<CosmeticDefinitionsResponse>(error)));

        await _config.DidNotReceive().GetAsync<Dictionary<string, DiscountDefinition>>(
            _context, "store.discounted-items");
    }

    [TestCase("not-found")]
    [TestCase("unknown")]
    public async Task Handle_DiscountLookupFailure_PropagatesError(string error)
    {
        SetResults(ApiResult.Ok(new Dictionary<string, CosmeticDefinition>()),
            ApiResult.Fail<Dictionary<string, DiscountDefinition>>(error));

        ApiResult<CosmeticDefinitionsResponse> result = await _module.Handle(_context);

        Assert.That(result, Is.EqualTo(ApiResult.Fail<CosmeticDefinitionsResponse>(error)));
    }

    private void SetResults(
        ApiResult<Dictionary<string, CosmeticDefinition>> items,
        ApiResult<Dictionary<string, DiscountDefinition>> discounts)
    {
        _config.GetAsync<Dictionary<string, CosmeticDefinition>>(_context, "store.cosmetics").Returns(items);
        _config.GetAsync<Dictionary<string, DiscountDefinition>>(_context, "store.discounted-items").Returns(discounts);
    }
}
