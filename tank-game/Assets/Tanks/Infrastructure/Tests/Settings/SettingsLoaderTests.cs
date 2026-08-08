using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Settings;

namespace Tanks.Infrastructure.Tests.Settings
{
    public class SettingsLoaderTests
    {
        private IFileSystem _fileSystem;
        private ISettingsLoader _loader;

        [SetUp]
        public void Setup()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _loader = new JsonSettingsLoader(_fileSystem);
        }

        [Test]
        public void Load_NoFile_ReturnsDefaultValues()
        {
            _fileSystem.Exists("no-file-path.json").Returns(false);

            Dictionary<PlayerSetting, int> result = _loader.Load("no-file-path.json");

            foreach (PlayerSetting setting in Enum.GetValues(typeof(PlayerSetting)))
            {
                if (result.TryGetValue(setting, out int defaultValue))
                {
                    Assert.AreEqual(JsonSettingsLoader.Defaults[setting], defaultValue);
                    continue;
                }

                Assert.Fail($"Setting '{setting}' is not specified in defaults");
            }
        }

        [Test]
        public void Load_InvalidFile_ReturnsDefaultValues()
        {
            _fileSystem.Exists("invalid-path.json").Returns(true);

            Dictionary<PlayerSetting, int> result = _loader.Load("invalid-path.json");

            foreach (PlayerSetting setting in Enum.GetValues(typeof(PlayerSetting)))
            {
                if (result.TryGetValue(setting, out int defaultValue))
                {
                    Assert.AreEqual(JsonSettingsLoader.Defaults[setting], defaultValue);
                    continue;
                }

                Assert.Fail($"Setting '{setting}' is not specified in defaults");
            }
        }

        [Test]
        public void Load_ValidFile_ReturnsSpecifiedValues()
        {
            PlayerSetting[] values = (PlayerSetting[])Enum.GetValues(typeof(PlayerSetting));

            var dictionaryToReturn = values.ToDictionary(x => x, x => x);
            string jsonToReturn = JsonConvert.SerializeObject(dictionaryToReturn);

            _fileSystem
                .Exists("some-file.json")
                .Returns(true);

            _fileSystem
                .ReadAllText("some-file.json")
                .Returns(jsonToReturn);

            Dictionary<PlayerSetting, int> result = _loader.Load("some-file.json");

            foreach (PlayerSetting setting in Enum.GetValues(typeof(PlayerSetting)))
            {
                int expected = (int)setting;
                int actual = result[setting];
                Assert.AreEqual(expected, actual, $"Invalid value at setting '{setting}'");
            }
        }
    }
}
