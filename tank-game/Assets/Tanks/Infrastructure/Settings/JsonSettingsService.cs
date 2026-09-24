using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tanks.Core.Settings;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.Infrastructure.Settings
{
    public sealed class JsonSettingsService : ISettingsService
    {
        public JsonSettingsService(ISettingsLoader loader)
        {
            _loader = loader;
        }

        public static readonly string FilePath = Path
            .Combine(Application.persistentDataPath, "settings.json");

        private readonly ISettingsLoader _loader;
        private readonly Dictionary<PlayerSetting, int> _newChanges = new();
        private readonly Dictionary<PlayerSetting, BindableProperty<int>> _settings = new();

        public void Write(PlayerSetting type, int value) => _newChanges[type] = value;

        public void RevertChanges()
        {
            _newChanges.Clear();

            foreach (BindableProperty<int> setting in _settings.Values)
            {
                // Mimics reset
                setting.ForceRefresh();
            }
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

            if (_settings is null || _settings.Count == 0)
            {
                foreach (var setting in result) _settings[setting.Key] = new(setting.Value);
                return;
            }

            foreach (var setting in result)
            {
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
            foreach (var setting in _newChanges) _settings[setting.Key].Value = setting.Value;
            _newChanges.Clear();

            var result = _settings.Select(x => KeyValuePair.Create(x.Key, x.Value.Value));
            _loader.Save(FilePath, result);
        }
    }
}
