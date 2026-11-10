using Backend.Models;
using Backend.Models.Store;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.RemoteConfig.Api;
using Unity.Services.RemoteConfig.Model;

namespace Backend.Tests.Services.Adapters;

public class RemoteConfigServiceTests
{
    private IExecutionContext _context = null!;
    private IRemoteConfigSettingsApi _api = null!;
    private RemoveConfigService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _context.AccessToken.Returns("token");
        _context.ProjectId.Returns("project");
        _context.EnvironmentId.Returns("environment");

        _api = Substitute.For<IRemoteConfigSettingsApi>();
        IGameApiClient client = Substitute.For<IGameApiClient>();
        client.RemoteConfigSettings.Returns(_api);
        _service = new RemoveConfigService(client);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAsync_DeserializesObjectOrNestedItem(bool nested)
    {
        JObject item = JObject.Parse("{\"CosmeticType\":\"body\",\"Conditions\":{\"Prices\":{\"pridePoints\":30,\"spareParts\":50}}}");
        SetSettings(new Dictionary<string, object>
        {
            ["store.cosmetics"] = nested ? new JObject { ["new"] = item } : item
        });

        ApiResult<CosmeticDefinition> result = await _service.GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", nested ? "new" : null);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.CosmeticType, Is.EqualTo("body"));
            Assert.That(result.Value.Conditions.Prices!["pridePoints"], Is.EqualTo(30));
            Assert.That(result.Value.Conditions.Prices["spareParts"], Is.EqualTo(50));
        });

        await _api.Received(1).AssignSettingsGetAsync(_context, "token", "project", "environment", null,
            Arg.Is<List<string>>(keys => keys.SequenceEqual(new[] { "store.cosmetics" })));
    }

    [Test]
    public async Task GetAsync_DictionaryValue_Deserializes()
    {
        SetSettings(new Dictionary<string, object>
        {
            ["store.cosmetics"] = new Dictionary<string, object>
            {
                ["new"] = new Dictionary<string, object>
                {
                    ["CosmeticType"] = "body",
                    ["Conditions"] = new Dictionary<string, object>
                    {
                        ["Prices"] = new Dictionary<string, object> { ["pridePoints"] = 30 }
                    }
                }
            }
        });

        ApiResult<CosmeticDefinition> result = await _service.GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", "new");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Conditions.Prices!["pridePoints"], Is.EqualTo(30));
        });
    }

    [Test]
    public async Task GetAsync_MissingKey_ReturnsNotFound()
    {
        SetSettings([]);

        ApiResult<CosmeticDefinition> result = await _service.GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", "new");

        Assert.That(result.ErrorCode, Is.EqualTo("not-found"));
    }

    [Test]
    public async Task GetAsync_DiscountByItemId_DeserializesPercentageAndExpiry()
    {
        DateTimeOffset endDate = new(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(5));
        SetSettings(new Dictionary<string, object>
        {
            ["store.discounted-items"] = new JObject
            {
                ["body-1"] = JObject.FromObject(new DiscountDefinition(25, endDate))
            }
        });

        ApiResult<DiscountDefinition> result = await _service.GetAsync<DiscountDefinition>(
            _context, "store.discounted-items", "body-1");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(new DiscountDefinition(25, endDate)));
        });
    }

    [Test]
    public async Task GetAsync_MissingSubKey_ReturnsNotFound()
    {
        SetSettings(new Dictionary<string, object> { ["store.cosmetics"] = new JObject() });

        ApiResult<CosmeticDefinition> result = await _service.GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", "new");

        Assert.That(result.ErrorCode, Is.EqualTo("not-found"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAsync_NullValue_ReturnsInvalidData(bool nested)
    {
        SetSettings(new Dictionary<string, object>
        {
            ["store.cosmetics"] = nested
                ? new JObject { ["new"] = JValue.CreateNull() }
                : null!
        });

        ApiResult<string> result = await _service.GetAsync<string>(
            _context, "store.cosmetics", nested ? "new" : null);

        Assert.That(result.ErrorCode, Is.EqualTo("invalid-data"));
    }

    [Test]
    public async Task GetAsync_ScalarValue_Deserializes()
    {
        SetSettings(new Dictionary<string, object> { ["price"] = 30 });

        ApiResult<int> result = await _service.GetAsync<int>(_context, "price");

        Assert.That(result, Is.EqualTo(ApiResult.Ok(30)));
    }

    [Test]
    public async Task GetAsync_NullValueType_ReturnsInvalidData()
    {
        SetSettings(new Dictionary<string, object> { ["price"] = JValue.CreateNull() });

        ApiResult<int> result = await _service.GetAsync<int>(_context, "price");

        Assert.That(result.ErrorCode, Is.EqualTo("invalid-data"));
    }

    [Test]
    public async Task GetAsync_IncompatibleData_ReturnsUnknown()
    {
        SetSettings(new Dictionary<string, object> { ["price"] = "invalid" });

        ApiResult<int> result = await _service.GetAsync<int>(_context, "price");

        Assert.That(result.ErrorCode, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task GetAsync_ApiException_ReturnsUnknownWithoutRetry()
    {
        _api.AssignSettingsGetAsync(_context, "token", "project", "environment", null,
            Arg.Any<List<string>>()).Returns(Task.FromException<ApiResponse<SettingsDeliveryResponse>>(
                new InvalidOperationException()));

        ApiResult<CosmeticDefinition> result = await _service.GetAsync<CosmeticDefinition>(
            _context, "store.cosmetics", "new");

        Assert.That(result.ErrorCode, Is.EqualTo("unknown"));
        await _api.Received(1).AssignSettingsGetAsync(_context, "token", "project", "environment", null,
            Arg.Any<List<string>>());
    }

    private void SetSettings(Dictionary<string, object> settings)
    {
        _api.AssignSettingsGetAsync(_context, "token", "project", "environment", null,
            Arg.Any<List<string>>()).Returns(ApiResponses.Create(
                new SettingsDeliveryResponse(new Configs(settings))));
    }
}
