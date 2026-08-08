using System.Collections.Generic;

namespace Tanks.Core.Settings
{
    public interface ISettingsLoader
    {
        Dictionary<PlayerSetting, int> Load(string filePath);
        void Save(string filePath, IEnumerable<KeyValuePair<PlayerSetting, int>> settings);
        void ResetToDefaults(string filePath);
    }
}
