using Tanks.Core.Lobbies;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class SessionConnectionService : ISessionConnectionService
    {
        public string IPAddress { get; private set; } = "127.0.0.1";
        public ushort Port { get; private set; } = 7777;

        public void SetConnectionData(string ipAdress, ushort port)
        {
            IPAddress = ipAdress;
            Port = port;
        }
    }
}
