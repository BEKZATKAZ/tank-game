using NUnit.Framework;
using System;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;

namespace Tanks.Localization.Tests
{
    public class LocalizedStringTests
    {
        private ILocalizationDatabase _database;

        [SetUp]
        public async Task Setup()
        {
            _database = new UnityLocalizationDatabase();
            await _database.WaitForInitializationAsync();
            _database.SetLocale("en");
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
        public async Task Get_DifferentLocales_ReturnsTranslated(
            string entryName, string expectedEn, string expectedRu)
        {
            _database.TryGet("Items", entryName, out ILocalizedString result);

            string stringFromCallback = null;
            void OnStringChanged(string newValue) => stringFromCallback = newValue;

            result.StringChanged += OnStringChanged;
            _database.SetLocale("ru");

            Assert.AreEqual(expectedRu, await result.GetAsync(), "Should switch from 'en' to 'ru'");
            Assert.AreEqual(expectedRu, result.Get(), "Should switch from 'en' to 'ru'");
            Assert.AreEqual(expectedRu, stringFromCallback, "Callback not called");

            _database.SetLocale("en");

            Assert.AreEqual(expectedEn, result.Get(), "Should switch from 'ru' to 'en'");
            Assert.AreEqual(expectedEn, stringFromCallback, "Callback not called");

            result.StringChanged -= OnStringChanged;

            _database.SetLocale("ru");
            Assert.AreEqual(expectedEn, stringFromCallback, "Callback should not be called");
        }

        [Test]
        public void AddVariable_UnsupportedTypes_Throws()
        {
            _database.TryGet("Items", "item.tier.uncommon", out ILocalizedString result);

            Assert.Throws<NotImplementedException>(
                () => result.AddVariable("name", (decimal)1));

            Assert.Throws<NotImplementedException>(
                () => result.AddVariable("name", new BigInteger(1)));
        }

        [TestCase("test")]
        [TestCase("var")]
        [TestCase("name")]
        public void AddVariable_ValidTypes_Success(string name)
        {
            _database.TryGet("Items", "item.tier.uncommon", out ILocalizedString result);

            result.AddVariable($"{name}_01", (byte)1);
            result.AddVariable($"{name}_02", (sbyte)1);

            result.AddVariable($"{name}_03", (short)1);
            result.AddVariable($"{name}_04", (ushort)1);

            result.AddVariable($"{name}_05", 1);
            result.AddVariable($"{name}_06", 1u);

            result.AddVariable($"{name}_07", 1L);
            result.AddVariable($"{name}_08", 1UL);

            result.AddVariable($"{name}_09", 1f);
            result.AddVariable($"{name}_010", 1d);

            result.AddVariable($"{name}_011", true);
            result.AddVariable($"{name}_012", false);

            result.AddVariable($"{name}_013", "str");
            result.AddVariable($"{name}_014", new Mesh());
        }
    }
}
