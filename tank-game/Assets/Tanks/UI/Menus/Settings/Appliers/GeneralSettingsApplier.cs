using System;
using Tanks.Core.Settings;
using Tanks.Localization;

namespace Tanks.UI.Menus.Settings
{
    public sealed class GeneralSettingsApplier : IDisposable
    {
        public GeneralSettingsApplier(
            ISettingsService settings,
            ILocalizationDatabase localization)
        {
            _settings = settings;
            _localization = localization;

            _settings.Bind(PlayerSetting.Language, ApplyGameLanguage);
        }

        private readonly ISettingsService _settings;
        private readonly ILocalizationDatabase _localization;

        private void ApplyGameLanguage(int index)
        {
            _localization.SetLocale((GameLanguage)index switch
            {
                GameLanguage.Russian => "ru",
                GameLanguage.French => "fr",
                GameLanguage.English or _ => "en"
            });
        }

        public void Dispose()
        {
            _settings.Unbind(PlayerSetting.Language, ApplyGameLanguage);
        }
    }
}