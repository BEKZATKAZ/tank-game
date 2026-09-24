using Cysharp.Threading.Tasks;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

using CachedLookups = System.Collections.Generic.Dictionary<
    (string tableName, string entryName),
    Tanks.Localization.ILocalizedString>;

namespace Tanks.Localization
{
    public struct StringLookup
    {
        public string TableName;
        public string EntryName;
    }

    public delegate void LocaleChangedEventHandler(string locale);

    public interface ILocalizationDatabase
    {
        event LocaleChangedEventHandler LocaleChanged;

        void SetLocale(string locale);
        UniTask WaitForInitializationAsync();

        bool TryGet(
            string tableName, string entryName, out ILocalizedString result);

        bool TryGet(
            string tableName, string entryName, out ILocalizedString result, out bool fromCache);
    }

    public sealed class UnityLocalizationDatabase : ILocalizationDatabase, IDisposable
    {
        public UnityLocalizationDatabase()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public event LocaleChangedEventHandler LocaleChanged;

        private readonly CachedLookups _cached = new();

        private void OnLocaleChanged(Locale locale)
        {
            LocaleChanged?.Invoke(locale.LocaleName);
        }

        public void Dispose()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        public void SetLocale(string locale)
        {
            LocalizationSettings.SelectedLocale =
                LocalizationSettings.AvailableLocales.GetLocale(locale);
        }

        public UniTask WaitForInitializationAsync()
        {
            return LocalizationSettings.InitializationOperation.ToUniTask();
        }

        public bool TryGet(string tableName, string entryName, out ILocalizedString result)
        {
            return TryGet(tableName, entryName, out result, out _);
        }

        public bool TryGet(
            string tableName, string entryName,
            out ILocalizedString result, out bool fromCache)
        {
            fromCache = false;

            (string tableName, string entryName) tuple = (tableName, entryName);

            if (_cached.TryGetValue(tuple, out result))
            {
                fromCache = true;
                return true;
            }

            StringTable table = LocalizationSettings.StringDatabase.GetTable(tableName);
            if (table == false) return false;

            StringTableEntry entry = table.GetEntry(entryName ?? "");
            if (entry is null) return false;

            result = new UnityLocalizedString(new LocalizedString(tableName, entryName));
            _cached.Add(tuple, result);

            fromCache = false;
            return true;
        }
    }
}
