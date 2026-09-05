using System.Collections.Generic;

namespace Tanks.Server
{
    public interface IConnectedClientService
    {
        int ConnectedClientCount { get; }

        IEnumerable<ulong> GetConnectedClientIds();
        ClientData GetClientData(ulong clientId);

        void AddClient(ulong clientId, ClientData data);
        void RemoveClient(ulong clientId);
    }

    public sealed class ConnectedClientService : IConnectedClientService
    {
        public int ConnectedClientCount => _clients.Count;

        private readonly Dictionary<ulong, ClientData> _clients = new();

        public IEnumerable<ulong> GetConnectedClientIds()
        {
            return _clients.Keys;
        }

        public ClientData GetClientData(ulong clientId)
        {
            return _clients[clientId];
        }

        public void AddClient(ulong clientId, ClientData data)
        {
            _clients.Add(clientId, data);
        }

        public void RemoveClient(ulong clientId)
        {
            _clients.Remove(clientId);
        }
    }
}
