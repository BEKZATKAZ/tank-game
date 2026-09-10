namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionSettingsService
    {
        ISessionSettings GetSettings();
        void SetSettings(ISessionSettings settings);
    }
}
