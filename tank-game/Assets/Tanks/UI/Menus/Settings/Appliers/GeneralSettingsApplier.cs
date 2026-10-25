using Cysharp.Threading.Tasks;
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
        private int _languageVersion;
        private bool _disposed;

        private void ApplyGameLanguage(int index)
        {
            if (_disposed) return;
            ApplyGameLanguageAsync(index, ++_languageVersion).Forget();
        }

        private async UniTask ApplyGameLanguageAsync(int index, int version)
        {
            await _localization.WaitForInitializationAsync();
            if (_disposed || version != _languageVersion) return;

            _localization.SetLocale((GameLanguage)index switch
            {
                GameLanguage.Russian => "ru",
                GameLanguage.French => "fr",
                GameLanguage.English or _ => "en"
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _settings.Unbind(PlayerSetting.Language, ApplyGameLanguage);
        }
    }
}
