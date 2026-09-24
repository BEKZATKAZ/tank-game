using System;

namespace Tanks.Core.Lobbies.Sessions.DTOs
{
    [Serializable]
    public struct DangerZoneSettingsFromLobby
    {
        public int StartDelaySeconds;
        public int DurationSeconds;
        public int DamagePerSecond;
    }
}
