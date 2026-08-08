using System;

namespace Tanks.Core.Settings
{
    public interface ISettingsService
    {
        void Bind(PlayerSetting type, Action<int> callback);
        void Unbind(PlayerSetting type, Action<int> callback);

        void Initialize();
        void Write(PlayerSetting type, int value);

        void RevertChanges();
        void ResetToDefaults();
        void ApplyChanges();
    }
}