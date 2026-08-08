namespace Tanks.Core.Settings
{
    // Jumping from 1 to 100 to make room for new settings
    public enum PlayerSetting
    {
        ScreenResolutionIndex = 1,
        FullscreenMode = 2,
        VSync = 3,
        FrameRateIndex = 4,

        MasterVolume = 100,
        MusicVolume = 101,
        SFXVolume = 102,
        UIVolume = 103,

        Language = 200
    }

    public enum GameLanguage
    {
        English,
        Russian
    }
}
