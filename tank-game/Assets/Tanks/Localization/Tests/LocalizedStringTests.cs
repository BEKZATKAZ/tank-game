using NUnit.Framework;
using System.Threading.Tasks;

namespace Tanks.Localization.Tests
{
    public class LocalizedStringTests
    {
        private ILocalizationDatabase _database;
        private ILocalizationLoader _loader;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _loader = new UnityLocalizationLoader();
            await _loader.WaitForInitializationAsync();
        }

        [SetUp]
        public void Setup()
        {
            _loader.SetLocale("en");
            _database = new UnityLocalizationDatabase();
        }

        [TestCase("Items", "invalid-test")]
        [TestCase("invalid-table", "item.tier.uncommon")]
        [TestCase("123abc", "123abc")]
        public void Get_InvalidEntry_ReturnsFalse(string tableName, string entryName)
        {
            bool exists = _database.TryGet(tableName, entryName, out ILocalizedString result);
            Assert.False(exists, "Invalid table/entry somehow exists");
            Assert.Null(result, "Returned false, but with a valid result");
        }

        [TestCase("item.tier.uncommon", "Uncommon")]
        [TestCase("item.tier.rare", "Rare")]
        [TestCase("item.tier.epic", "Epic")]
        public void Get_Valid_ReturnsValidEntry(string entryName, string expected)
        {
            bool exists1 = _database.TryGet("Items", entryName, out var result, out var fromCache);

            Assert.True(exists1);
            Assert.AreEqual(expected, result.Get());
            Assert.False(fromCache, "Looks like the database had cached something before");

            bool exists2 = _database.TryGet("Items", entryName, out var other, out fromCache);

            Assert.True(exists2);
            Assert.AreEqual(result, other, "Second result should come from cache");
            Assert.True(fromCache, "Should come from cache");
        }

        [TestCase("item.tier.uncommon", "Uncommon", "Необычный")]
        [TestCase("item.tier.rare", "Rare", "Редкий")]
        [TestCase("item.tier.epic", "Epic", "Эпический")]
        public void Get_DifferentLocales_ReturnsTranslated(
            string entryName, string expectedEn, string expectedRu)
        {
            _database.TryGet("Items", entryName, out ILocalizedString result);
            string stringFromCallback = null;
            result.StringChanged += x => stringFromCallback = x;

            _loader.SetLocale("ru");
            Assert.AreEqual(expectedRu, result.Get(), "Should switch from 'en' to 'ru'");
            Assert.AreEqual(expectedRu, stringFromCallback, "Callback not called");

            _loader.SetLocale("en");
            Assert.AreEqual(expectedEn, result.Get(), "Should switch from 'ru' to 'en'");
            Assert.AreEqual(expectedEn, stringFromCallback, "Callback not called");
        }
    }
}
