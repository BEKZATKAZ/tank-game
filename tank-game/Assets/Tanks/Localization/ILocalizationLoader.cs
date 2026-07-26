using Cysharp.Threading.Tasks;
using UnityEngine.Localization.Settings;

namespace Tanks.Localization
{
    public interface ILocalizationLoader
    {
        UniTask WaitForInitializationAsync();

        void SetLocale(string locale);
    }

    public sealed class UnityLocalizationLoader : ILocalizationLoader
    {
        public void SetLocale(string locale)
        {
            LocalizationSettings.SelectedLocale =
                LocalizationSettings.AvailableLocales.GetLocale(locale);
        }

        public UniTask WaitForInitializationAsync()
        {
            return LocalizationSettings.InitializationOperation.ToUniTask();
        }
    }
}
