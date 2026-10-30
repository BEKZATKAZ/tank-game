using System;

namespace Tanks.Core.Lobbies.Sessions.DTOs
{
    [Serializable]
    public struct PlayerStatSettingsFromLobby
    {
        public int ForwardMovementSpeed;
        public int BackwardsMovementSpeed;
    }
}