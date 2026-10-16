using FMOD.Studio;
using FMODUnity;
using System;
using Tanks.Shared.Adapters;

namespace Tanks.Audio
{
    public interface IAudioInstance : IDisposable
    {
        void Play();
        void Stop(bool fadeOut = true);
        void SetVolume(float volume);

        void AttachTo(ITransform target);
        void SetParameter(string name, float value);
    }

    public sealed class FMODAudioInstance : IAudioInstance
    {
        public FMODAudioInstance(EventInstance instance) => _instance = instance;

        private readonly EventInstance _instance;

        public void Dispose()
        {
            _instance.release();
        }

        public void Play()
        {
            _instance.start();
        }

        public void Stop(bool fadeOut = true)
        {
            _instance.stop(fadeOut
                ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
                : FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        public void AttachTo(ITransform target)
        {
            if (target is not UnityTransform transform) return;
            RuntimeManager.AttachInstanceToGameObject(_instance, transform.Transform.gameObject);
        }

        public void SetParameter(string name, float value)
        {
            _instance.setParameterByName(name, value);
        }

        public void SetVolume(float volume)
        {
            _instance.setVolume(volume);
        }
    }
}
