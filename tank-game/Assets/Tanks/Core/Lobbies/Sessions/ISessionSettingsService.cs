namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionSettingsService
    {
        ISessionDefaultSettingsConfig DefaultSettings { get; }

        ISessionSettings GetSettings();
        void SetSettings(ISessionSettings settings);
    }
}
