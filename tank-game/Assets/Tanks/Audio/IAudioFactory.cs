using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Tanks.Audio
{
    public interface IAudioFactory
    {
        IAudioInstance CreateInstance(string path);
    }

    public sealed class FMODAudioFactory : IAudioFactory
    {
        public IAudioInstance CreateInstance(string path)
        {
            EventInstance instance = RuntimeManager.CreateInstance(path);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(Vector3.zero));
            return new FMODAudioInstance(instance);
        }
    }
}
