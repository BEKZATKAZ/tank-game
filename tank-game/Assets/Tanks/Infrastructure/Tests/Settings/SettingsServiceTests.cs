using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Infrastructure.Settings;

namespace Tanks.Infrastructure.Tests.Settings
{
    public class SettingsServiceTests
    {
        private IFileSystem _fileSystem;
        private ISettingsLoader _loader;
        private ISettingsService _service;

        [SetUp]
        public void Setup()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _loader = new JsonSettingsLoader(_fileSystem);
            _service = new JsonSettingsService(_loader);

            _fileSystem
                .ReadAllText(JsonSettingsService.FilePath)
                .Returns("");
        }

        [Test]
        public void Initialize_LoadsFromJsonFile()
        {
            _fileSystem.Exists(JsonSettingsService.FilePath).Returns(true);
            _service.Initialize();

            _fileSystem.Received(1).Exists(JsonSettingsService.FilePath);
            _fileSystem.Received(1).ReadAllText(JsonSettingsService.FilePath);
        }

        [Test]
        public void Bind_WorksAfterInitializing()
        {
            int result = 0;

            void Binding(int x) => result = x;

            Assert.Throws<KeyNotFoundException>(() =>
                _service.Bind(PlayerSetting.MasterVolume, Binding));

            _service.Initialize();
            _service.Bind(PlayerSetting.MasterVolume, Binding);

            Assert.AreEqual(JsonSettingsLoader.Defaults[PlayerSetting.MasterVolume], result);
        }

        [Test]
        public void Apply_SavesSettings()
        {
            _service.Initialize();

            float music = 0;
            float sfx = 0;

            _service.Write(PlayerSetting.MusicVolume, 50);
            _service.Write(PlayerSetting.SFXVolume, 10);

            _service.Bind(PlayerSetting.MusicVolume, x => music = x);
            _service.Bind(PlayerSetting.SFXVolume, x => sfx = x);
            _service.ApplyChanges();

            Assert.AreEqual(50, music, "New changes should be applied by now");
            Assert.AreEqual(10, sfx, "New changes should be applied by now");

            _service.Write(PlayerSetting.MusicVolume, 20);
            _service.RevertChanges();
            _service.ApplyChanges();

            Assert.AreEqual(50, music, "Should not update, changes were canceled");
            Assert.AreEqual(10, sfx, "Should not update, changes were canceled");
        }
    }
}
