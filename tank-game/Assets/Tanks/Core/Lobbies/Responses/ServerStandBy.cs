using System;
using Tanks.Core.Lobbies.Sessions;

namespace Tanks.Core.Lobbies.Responses
{
    [Serializable]
    public struct ServerStandBy
    {
        public string JoinCode;
        public SessionSettings Settings;
    }
}
