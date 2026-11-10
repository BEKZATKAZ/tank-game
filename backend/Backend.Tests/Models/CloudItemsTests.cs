using Backend.Models;
using Backend.Models.Economy;
using Backend.Tests.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Models;

public class CloudItemsTests
{
    [Test]
    public void As_CachesDeserializedValueAndPreservesMutations()
    {
        CloudItemEntry entry = CreateEntry("[\"existing\"]");
        List<string> cosmetics = entry.As<List<string>>();

        cosmetics.Add("new");

        Assert.Multiple(() =>
        {
            Assert.That(entry.As<List<string>>(), Is.SameAs(cosmetics));
            Assert.That(entry.GetValue(), Is.SameAs(cosmetics));
            Assert.That(cosmetics, Is.EqualTo(new[] { "existing", "new" }));
        });
    }

    [Test]
    public void As_NullJson_UsesAndCachesFallback()
    {
        CloudItemEntry entry = CreateEntry("null");
        List<string> fallback = [];

        Assert.Multiple(() =>
        {
            Assert.That(entry.As(fallback), Is.SameAs(fallback));
            Assert.That(entry.GetValue(), Is.SameAs(fallback));
        });
    }

    [Test]
    public void As_NullWithoutFallback_Throws()
    {
        Assert.Throws<NullReferenceException>(() => CreateEntry("null").As<List<string>>());
    }

    [Test]
    public void As_InvalidJson_Throws()
    {
        Assert.Catch<JsonException>(() => CreateEntry("{").As<PlayerBalance>());
    }

    [Test]
    public void GetSetters_PreservesUntouchedJsonAndWriteLocks()
    {
        CloudItems items = new([
            CloudSaveItems.Create("cosmetics", JArray.Parse("[\"existing\"]"), "cosmetics-lock"),
            CloudSaveItems.Create("balance", JObject.Parse("{\"PridePoints\":100}"), "balance-lock")
        ]);

        List<SetItemBody> setters = items.GetSetters();

        Assert.Multiple(() =>
        {
            Assert.That(setters, Has.Count.EqualTo(2));
            Assert.That(setters.Single(x => x.Key == "cosmetics").WriteLock,
                Is.EqualTo("cosmetics-lock"));

            Assert.That(setters.Single(x => x.Key == "balance").WriteLock,
                Is.EqualTo("balance-lock"));

            Assert.That(JToken.FromObject(setters.Single(x => x.Key == "cosmetics").Value)
                .ToObject<List<string>>(), Is.EqualTo(new[] { "existing" }));

            Assert.That(JToken.FromObject(setters.Single(x => x.Key == "balance").Value)
                .Value<int>("PridePoints"), Is.EqualTo(100));
        });
    }

    [Test]
    public void GetSetters_ContainsMutatedTypedValues()
    {
        CloudItems items = new([
            CloudSaveItems.Create("cosmetics", JArray.Parse("[]"), "cosmetics-lock"),
            CloudSaveItems.Create("balance", JObject.Parse("{\"PridePoints\":100}"), "balance-lock")
        ]);

        items.Get<List<string>>("cosmetics").Add("new");
        Assert.That(items.Get<PlayerBalance>("balance")
            .TryConsume(CurrencyType.PridePoints, 30), Is.True);

        List<SetItemBody> setters = items.GetSetters();

        Assert.Multiple(() =>
        {
            Assert.That(setters.Single(x => x.Key == "cosmetics").Value,
                Is.EqualTo(new[] { "new" }));

            Assert.That(((PlayerBalance)setters.Single(x => x.Key == "balance").Value)
                .PridePoints, Is.EqualTo(70));
        });
    }

    [Test]
    public void Get_MissingKey_RegistersFallbackWithoutWriteLock()
    {
        CloudItems items = new([]);
        List<string> fallback = [];

        List<string> cosmetics = items.Get("cosmetics", fallback);
        cosmetics.Add("new");

        SetItemBody setter = items.GetSetters().Single();

        Assert.Multiple(() =>
        {
            Assert.That(items.Get<List<string>>("cosmetics"), Is.SameAs(fallback));
            Assert.That(setter.Key, Is.EqualTo("cosmetics"));
            Assert.That(setter.Value, Is.SameAs(fallback));
            Assert.That(setter.WriteLock, Is.Null);
        });
    }

    [Test]
    public void Get_MissingKeyWithoutFallback_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => new CloudItems([]).Get<object>("missing"));
    }

    [Test]
    public void Get_NullValue_UsesFallbackAndPreservesWriteLock()
    {
        CloudItems items = new([CloudSaveItems.Create("cosmetics", JValue.CreateNull(), "lock")]);
        List<string> fallback = [];

        Assert.Multiple(() =>
        {
            Assert.That(items.Get("cosmetics", fallback), Is.SameAs(fallback));
            Assert.That(items.GetSetters().Single().WriteLock, Is.EqualTo("lock"));
        });
    }

    [Test]
    public void GetSetters_EmptyItems_ReturnsEmptyList()
    {
        Assert.That(new CloudItems([]).GetSetters(), Is.Empty);
    }

    [Test]
    public void Get_StringValue_PreservesJsonEscaping()
    {
        string value = "a \"quoted\" value\nnext line";
        CloudItems items = new([CloudSaveItems.Create("name", value, "lock")]);

        Assert.Multiple(() =>
        {
            Assert.That(items.Get<string>("name"), Is.EqualTo(value));
            Assert.That(items.GetSetters().Single().Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void GetValue_WithoutTypedRead_CachesParsedValue()
    {
        CloudItemEntry entry = CreateEntry("{\"count\":3}");

        object value = entry.GetValue();

        Assert.Multiple(() =>
        {
            Assert.That(entry.GetValue(), Is.SameAs(value));
            Assert.That(JToken.FromObject(value).Value<int>("count"), Is.EqualTo(3));
        });
    }

    private static CloudItemEntry CreateEntry(string json)
    {
        return new CloudItemEntry { InitialJson = json, WriteLock = "lock" };
    }
}
