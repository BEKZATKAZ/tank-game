using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Core.Settings;
using Tanks.UI.Menus.Settings;

namespace Tanks.UI.Tests.Menus
{
    public class SettingsTests : ViewModelTestBase<SettingsViewModel>
    {
        private ISettingsService _service;

        protected override SettingsViewModel CreateViewModel()
        {
            _service = Substitute.For<ISettingsService>();
            return new SettingsViewModel(_service);
        }

        [Test]
        public void Bind_UsesSelectedSetting()
        {
            Action<int> callback = _ => { };
            ViewModel.SetBindTarget(PlayerSetting.MusicVolume);
            ViewModel.Bind(callback);
            _service.Received(1).Bind(PlayerSetting.MusicVolume, callback);
        }

        [Test]
        public void Unbind_UsesSelectedSetting()
        {
            Action<int> callback = _ => { };
            ViewModel.SetBindTarget(PlayerSetting.MusicVolume);

            ViewModel.Unbind(callback);

            _service.Received(1).Unbind(PlayerSetting.MusicVolume, callback);
        }

        [Test]
        public void Write_EnablesButtons()
        {
            ViewModel.Write(PlayerSetting.MasterVolume, 50);
            _service.Received(1).Write(PlayerSetting.MasterVolume, 50);
            Assert.IsTrue(ViewModel.ApplyButtonEnabled.Value);
            Assert.IsTrue(ViewModel.RevertButtonEnabled.Value);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FinishEditing_DisablesButtons(int action)
        {
            ViewModel.Write(PlayerSetting.MasterVolume, 50);

            switch (action)
            {
                case 0: ViewModel.ApplyChanges(); _service.Received(1).ApplyChanges(); break;
                case 1: ViewModel.RevertChanges(); _service.Received(1).RevertChanges(); break;
                case 2: ViewModel.ResetToDefaults(); _service.Received(1).ResetToDefaults(); break;
            }

            Assert.IsFalse(ViewModel.ApplyButtonEnabled.Value);
            Assert.IsFalse(ViewModel.RevertButtonEnabled.Value);
        }
    }
}
