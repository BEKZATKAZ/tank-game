using VContainer;

namespace Tanks.Localization
{
    public static class LocalizationScope
    {
        public static RegistrationBuilder RegisterLocalization
            (this IContainerBuilder builder, Lifetime lifetime)
        {
            return builder
                .Register<ILocalizationDatabase, UnityLocalizationDatabase>(lifetime);
        }
    }
}
