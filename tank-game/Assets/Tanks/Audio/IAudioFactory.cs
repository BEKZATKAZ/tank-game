using FMODUnity;

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
            return new FMODAudioInstance(RuntimeManager.CreateInstance(path));
        }
    }
}
