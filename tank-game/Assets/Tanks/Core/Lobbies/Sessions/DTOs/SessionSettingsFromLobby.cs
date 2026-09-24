using System;

namespace Tanks.Core.Lobbies.Sessions.DTOs
{
    [Serializable]
    public struct SessionSettingsFromLobby
    {
        public int PlayerCount;
        public int MaxScore;

        public MapGenerationSettingsFromLobby MapGeneration;
        public ItemSpawnSettingsFromLobby ItemSpawn;
        public DangerZoneSettingsFromLobby DangerZone;
    }
}
