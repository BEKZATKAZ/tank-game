using System;
using Tanks.Core.Lobbies.Sessions.DTOs;

namespace Tanks.Core.Lobbies.Payloads
{
    [Serializable]
    public struct PrepareServerRequest
    {
        public SessionSettingsFromLobby Settings;
        public bool IsLocalhost;
    }
}
