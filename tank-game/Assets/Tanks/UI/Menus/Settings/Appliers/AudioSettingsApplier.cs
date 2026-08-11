using System;
using Tanks.Audio;
using Tanks.Core.Settings;

namespace Tanks.UI.Menus.Settings.Appliers
{
    public sealed class AudioSettingsApplier : IDisposable
    {
        public AudioSettingsApplier(ISettingsService settings, IAudioMixer mixer)
        {
            _settings = settings;
            _mixer = mixer;

            _settings.Bind(PlayerSetting.MasterVolume, ApplyMasterVolume);
            _settings.Bind(PlayerSetting.MusicVolume, ApplyMusicVolume);
            _settings.Bind(PlayerSetting.SFXVolume, ApplySPXVolume);
            _settings.Bind(PlayerSetting.UIVolume, ApplyUIVolume);
        }

        private readonly ISettingsService _settings;
        private readonly IAudioMixer _mixer;

        public void Dispose()
        {
            _settings.Unbind(PlayerSetting.MasterVolume, ApplyMasterVolume);
            _settings.Unbind(PlayerSetting.MusicVolume, ApplyMusicVolume);
            _settings.Unbind(PlayerSetting.SFXVolume, ApplySPXVolume);
            _settings.Unbind(PlayerSetting.UIVolume, ApplyUIVolume);
        }

        private void ApplyMasterVolume(int volume) => _mixer.SetMasterVolume(volume * 0.01f);
        private void ApplyMusicVolume(int volume) => _mixer.SetMusicVolume(volume * 0.01f);
        private void ApplySPXVolume(int volume) => _mixer.SetSFXVolume(volume * 0.01f);
        private void ApplyUIVolume(int volume) => _mixer.SetUIVolume(volume * 0.01f);
    }
}
