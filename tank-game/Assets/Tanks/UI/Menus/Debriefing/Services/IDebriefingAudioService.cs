namespace Tanks.UI.Menus.Debriefing.Services
{
    public interface IDebriefingAudioService
    {
        void Configure(string statPath, string levelPath, float maxPitch);
        void SetProgress(float progress);
        void SetCounting(bool active);
        void LevelUp();
        void Stop();
    }
}
