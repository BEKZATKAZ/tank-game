using Backend.Models;
using Backend.Models.Leveling;
using Backend.Modules;
using Backend.Services.Adapters;
using Backend.Tests.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Modules;

public class GetLocalPlayerDataModuleTests
{
    private IExecutionContext _context = null!;
    private ICloudSaveService _save = null!;
    private GetLocalPlayerDataModule _module = null!;

    [SetUp]
    public void SetUp()
    {
        _context = Substitute.For<IExecutionContext>();
        _save = Substitute.For<ICloudSaveService>();
        _module = new GetLocalPlayerDataModule(_save);
    }

    [Test]
    public async Task Handle_Success_HydratesAndSerializesPlayerDataWithoutSaving()
    {
        CloudItems player = new([
            CloudSaveItems.Create("level", JObject.FromObject(new { Value = 7, XP = 450 }), "level-lock"),
            CloudSaveItems.Create("balance", JObject.FromObject(new { PridePoints = 100, SpareParts = 300 }), "balance-lock"),
            CloudSaveItems.Create("cosmetics", new JArray("body-1"), "cosmetics-lock")
        ]);

        SetPlayer(player);

        ApiResult<PlayerDTO> result = await _module.Handle(_context);

        string json = JsonConvert.SerializeObject(result);
        ApiResult<PlayerDTO> restored = JsonConvert.DeserializeObject<ApiResult<PlayerDTO>>(json);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Level.Value, Is.EqualTo(7));
            Assert.That(result.Value.Level.XP, Is.EqualTo(450));
            Assert.That(result.Value.Balance.PridePoints, Is.EqualTo(100));
            Assert.That(result.Value.Balance.SpareParts, Is.EqualTo(300));
            Assert.That(result.Value.Cosmetics, Is.EqualTo(new[] { "body-1" }));
            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Value.Level.Value, Is.EqualTo(7));
            Assert.That(restored.Value.Level.XP, Is.EqualTo(450));
            Assert.That(restored.Value.Balance.SpareParts, Is.EqualTo(300));
            Assert.That(restored.Value.Cosmetics, Is.EqualTo(new[] { "body-1" }));
            Assert.That(player.GetSetters().Single(item => item.Key == "level").WriteLock, Is.EqualTo("level-lock"));
        });

        await _save.Received(1).LoadItemsAsync(_context,
            Arg.Is<List<string>>(keys => keys.SequenceEqual(new[] { "level", "balance", "cosmetics", "equippedCosmetics", "lastClaimedTimes" })));

        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Handle_MissingOrNullKeys_UsesDefaultsWithoutSaving(bool nullValues)
    {
        List<Item> items = nullValues
            ? [CloudSaveItems.Create("level", JValue.CreateNull(), "level-lock"),
                CloudSaveItems.Create("balance", JValue.CreateNull(), "balance-lock"),
                CloudSaveItems.Create("cosmetics", JValue.CreateNull(), "cosmetics-lock")]
            : [];

        SetPlayer(new CloudItems(items));

        ApiResult<PlayerDTO> result = await _module.Handle(_context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Level.Value, Is.EqualTo(new Level().Value));
            Assert.That(result.Value.Level.XP, Is.Zero);
            Assert.That(result.Value.Balance.PridePoints, Is.Zero);
            Assert.That(result.Value.Balance.SpareParts, Is.Zero);
            Assert.That(result.Value.Cosmetics, Is.Empty);
        });

        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    [TestCase("unauthorized")]
    [TestCase("unknown")]
    public async Task Handle_LoadFailure_ReturnsErrorWithoutReadingOrSaving(string error)
    {
        _save.LoadItemsAsync(_context, Arg.Any<List<string>>()).Returns(ApiResult.Fail<CloudItems>(error));

        ApiResult<PlayerDTO> result = await _module.Handle(_context);

        Assert.That(result, Is.EqualTo(ApiResult.Fail<PlayerDTO>(error)));

        await _save.DidNotReceive().SaveItemsAsync(_context, Arg.Any<CloudItems>());
    }

    private void SetPlayer(CloudItems player)
    {
        _save.LoadItemsAsync(_context, Arg.Any<List<string>>()).Returns(ApiResult.Ok(player));
    }
}
