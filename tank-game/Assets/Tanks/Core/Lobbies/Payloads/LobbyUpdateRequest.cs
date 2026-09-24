using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;

namespace Tanks.Core.Lobbies.Payloads
{
    public struct LobbyUpdateRequest
    {
        public string Name;
        public string JoinCode;
        public LobbyStatus? Status;
        public SessionSettingsFromLobby? Settings;
        public SessionSettings? ServerConfirmedSettings;
    }
}
