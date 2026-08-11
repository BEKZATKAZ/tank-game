using VContainer;

namespace Tanks.Audio
{
    public static class AudioScope
    {
        public static void RegisterAudio(
            this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<IAudioFactory, FMODAudioFactory>(lifetime);
            builder.Register<IAudioPlayer, FMODAudioPlayer>(lifetime);
            builder.Register<IAudioMixer, FMODAudioMixer>(lifetime);
        }
    }
}
