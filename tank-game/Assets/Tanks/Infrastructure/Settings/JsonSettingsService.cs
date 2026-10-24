using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tanks.Core.Settings;
using Tanks.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Tanks.Infrastructure.Settings
{
    public sealed partial class JsonSettingsService : ISettingsService
    {
        public JsonSettingsService(ISettingsLoader loader)
        {
            _loader = loader;
        }

        [AutoStaticsCleanup]
        public static readonly string FilePath = Path
            .Combine(Application.persistentDataPath, "settings.json");

        private readonly ISettingsLoader _loader;
        private readonly Dictionary<PlayerSetting, int> _oldValues = new();
        private readonly Dictionary<PlayerSetting, int> _newChanges = new();
        private readonly Dictionary<PlayerSetting, BindableProperty<int>> _settings = new();

        public void Write(PlayerSetting type, int value) => _newChanges[type] = value;

        public void RevertChanges()
        {
            foreach (var newChange in _newChanges.ToDictionary(k => k.Key, v => v.Value))
            {
                if (_oldValues.TryGetValue(newChange.Key, out int oldValue) == false)
                {
                    oldValue = _settings[newChange.Key].Value;
                }

                int newValue = newChange.Value;

                if (oldValue == newValue)
                {
                    // Mimics reset
                    _settings[newChange.Key].ForceRefresh();
                    continue;
                }

                _settings[newChange.Key].Value = oldValue;
            }
        }

        public void PreviewChanges(PlayerSetting setting, int newValue)
        {
            _oldValues.TryAdd(setting, _settings[setting].Value);
            _newChanges[setting] = newValue;
            _settings[setting].Value = newValue;
        }

        public void Bind(PlayerSetting type, Action<int> callback)
        {
            _settings[type].Bind(callback);
        }

        public void Unbind(PlayerSetting type, Action<int> callback)
        {
            _settings[type].Unbind(callback);
        }

        public void Initialize()
        {
            Dictionary<PlayerSetting, int> result = _loader.Load(FilePath);

            if (_settings.Count == 0)
            {
                foreach (var setting in result)
                {
                    _settings[setting.Key] = new BindableProperty<int>(setting.Value);
                    _oldValues[setting.Key] = setting.Value;
                }

                return;
            }

            foreach (var setting in result)
            {
                _oldValues[setting.Key] = setting.Value;
                _settings[setting.Key].Value = setting.Value;
            }
        }

        public void ResetToDefaults()
        {
            _loader.ResetToDefaults(FilePath);
            Initialize();
        }

        public void ApplyChanges()
        {
            foreach (var setting in _newChanges)
            {
                _settings[setting.Key].Value = setting.Value;
                _oldValues[setting.Key] = setting.Value;
            }

            _newChanges.Clear();

            var result = _settings.Select(x => KeyValuePair.Create(x.Key, x.Value.Value));
            _loader.Save(FilePath, result);
        }
    }
}
