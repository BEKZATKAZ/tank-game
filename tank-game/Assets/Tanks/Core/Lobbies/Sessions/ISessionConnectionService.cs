namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionConnectionService
    {
        string IPAddress { get; }
        ushort Port { get; }

        void SetConnectionData(string ipAdress, ushort port);
    }
}
