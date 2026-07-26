using VContainer;

namespace Tanks.Localization
{
    public static class LocalizationScope
    {
        public static RegistrationBuilder RegisterLocalization
            (this IContainerBuilder builder, Lifetime lifetime)
        {
            builder.Register<ILocalizationLoader, UnityLocalizationLoader>(lifetime);

            return builder
                .Register<ILocalizationDatabase, UnityLocalizationDatabase>(lifetime);
        }
    }
}
