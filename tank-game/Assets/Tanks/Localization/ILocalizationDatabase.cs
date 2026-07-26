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

    public interface ILocalizationDatabase
    {
        bool TryGet(
            string tableName, string entryName, out ILocalizedString result);

        bool TryGet(
            string tableName, string entryName, out ILocalizedString result, out bool fromCache);
    }

    public sealed class UnityLocalizationDatabase : ILocalizationDatabase
    {
        private readonly CachedLookups _cached = new();

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

            StringTableEntry entry = table.GetEntry(entryName);
            if (entry is null) return false;

            result = new UnityLocalizedString(new LocalizedString(tableName, entryName));
            _cached.Add(tuple, result);

            fromCache = false;
            return true;
        }
    }
}
