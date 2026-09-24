using System;

namespace Tanks.Core.Lobbies.Responses
{
    [Serializable]
    public struct ServerAuthReady
    {
        public string AuthCode;
    }
}
