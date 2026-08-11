using FMOD.Studio;
using FMODUnity;

namespace Tanks.Audio
{
    public interface IAudioMixer
    {
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
        void SetUIVolume(float volume);
    }

    public sealed class FMODAudioMixer : IAudioMixer
    {
        public FMODAudioMixer()
        {
            _master = RuntimeManager.GetBus("bus:/");
            _music = RuntimeManager.GetBus("bus:/Music");
            _sfx = RuntimeManager.GetBus("bus:/SFX");
            _ui = RuntimeManager.GetBus("bus:/UI");
        }

        private readonly Bus _master;
        private readonly Bus _music;
        private readonly Bus _sfx;
        private readonly Bus _ui;

        public void SetMasterVolume(float volume)
        {
            _master.setVolume(volume);
        }

        public void SetMusicVolume(float volume)
        {
            _music.setVolume(volume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfx.setVolume(volume);
        }

        public void SetUIVolume(float volume)
        {
            _ui.setVolume(volume);
        }
    }
}
