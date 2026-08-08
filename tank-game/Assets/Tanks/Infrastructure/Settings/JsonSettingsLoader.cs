using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using UnityEngine;

namespace Tanks.Infrastructure.Settings
{
    public sealed class JsonSettingsLoader : ISettingsLoader
    {
        public JsonSettingsLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public static readonly IReadOnlyDictionary<PlayerSetting, int> Defaults =
            new Dictionary<PlayerSetting, int>()
            {
                { PlayerSetting.ScreenResolutionIndex, GetDefaultResolutionIndex() },
                { PlayerSetting.FullscreenMode, (int)FullScreenMode.FullScreenWindow },
                { PlayerSetting.VSync, 0 },
                { PlayerSetting.FrameRateIndex, 1 },

                { PlayerSetting.MasterVolume, 100 },
                { PlayerSetting.MusicVolume, 80 },
                { PlayerSetting.SFXVolume, 80 },
                { PlayerSetting.UIVolume, 80 },

                { PlayerSetting.Language, GetDefaultLanguage() }
            };

        private readonly IFileSystem _fileSystem;

        private static int GetDefaultResolutionIndex()
        {
            return Screen.resolutions.ToList().IndexOf(Screen.currentResolution);
        }

        private static int GetDefaultLanguage()
        {
            string language = Application.systemLanguage.ToString();

            if (Enum.TryParse(language, out GameLanguage result)) return (int)result;

            return (int)GameLanguage.English;
        }

        public Dictionary<PlayerSetting, int> Load(string filePath)
        {
            Dictionary<PlayerSetting, int> result = new(Defaults);

            try
            {
                // Create file with defaults
                if (_fileSystem.Exists(filePath) == false)
                {
                    ResetToDefaults(filePath);
                    return result;
                }

                string json = _fileSystem.ReadAllText(filePath) ?? "";

                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                if (deserialized is null) return result;

                // Apply
                foreach (var setting in deserialized)
                {
                    if (Enum.TryParse(setting.Key, out PlayerSetting type) == false)
                    {
                        Debug.LogWarningFormat("Unsupported setting '{0}'", type);
                        continue;
                    }

                    result[type] = setting.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return result;
        }

        public void Save(string filePath, IEnumerable<KeyValuePair<PlayerSetting, int>> settings)
        {
            try
            {
                Dictionary<string, int> toSave = settings
                    .ToDictionary(k => k.Key.ToString(), v => v.Value);

                string json = JsonConvert.SerializeObject(toSave, Formatting.Indented);
                _fileSystem.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void ResetToDefaults(string filePath)
        {
            Save(filePath, Defaults);
        }
    }
}
