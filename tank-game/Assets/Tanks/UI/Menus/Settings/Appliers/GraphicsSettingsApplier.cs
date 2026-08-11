using System;
using Tanks.Core.Settings;
using UnityEngine;

namespace Tanks.UI.Menus.Settings
{
    public sealed class GraphicsSettingsApplier : IDisposable
    {
        public GraphicsSettingsApplier(ISettingsService settings)
        {
            _settings = settings;
            _settings.Bind(PlayerSetting.ScreenResolutionIndex, ApplyScreenResolution);
            _settings.Bind(PlayerSetting.FullscreenMode, ApplyFullscreenMode);
            _settings.Bind(PlayerSetting.VSync, ApplyVSync);
            _settings.Bind(PlayerSetting.FrameRateIndex, ApplyFrameRate);
        }

        private readonly ISettingsService _settings;

        public void Dispose()
        {
            _settings.Unbind(PlayerSetting.ScreenResolutionIndex, ApplyScreenResolution);
            _settings.Unbind(PlayerSetting.FullscreenMode, ApplyFullscreenMode);
            _settings.Unbind(PlayerSetting.VSync, ApplyVSync);
            _settings.Unbind(PlayerSetting.FrameRateIndex, ApplyFrameRate);
        }

        private void ApplyScreenResolution(int index)
        {
            Resolution resolution = Screen.resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        }

        private void ApplyFullscreenMode(int index)
        {
            Screen.fullScreenMode = index switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => Screen.fullScreenMode
            };
        }

        private void ApplyVSync(int index)
        {
            QualitySettings.vSyncCount = index;
        }

        private void ApplyFrameRate(int index)
        {
            Application.targetFrameRate = index switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 240,
                _ => Application.targetFrameRate
            };
        }
    }
}
