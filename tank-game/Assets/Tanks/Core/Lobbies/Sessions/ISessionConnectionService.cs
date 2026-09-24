namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionConnectionService
    {
        string JoinCode { get; }

        void SetConnectionData(string joinCode);
    }
}
