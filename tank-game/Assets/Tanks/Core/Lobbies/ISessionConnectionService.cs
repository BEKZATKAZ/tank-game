namespace Tanks.Core.Lobbies
{
    public interface ISessionConnectionService
    {
        string IPAddress { get; }
        ushort Port { get; }

        void SetConnectionData(string ipAdress, ushort port);
    }
}
