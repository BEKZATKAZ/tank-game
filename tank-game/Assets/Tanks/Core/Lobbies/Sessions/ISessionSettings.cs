using System;
using UnityEngine;

namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionSettings
    {
        int DesiredPlayerCount { get; }
        MapGenerationSettings MapGeneration { get; }
        ItemSpawnSettings ItemSpawn { get; }
        DangerZoneSettings DangerZone { get; }
    }

    [Serializable]
    public struct MapGenerationSettings
    {
        public Vector2Int Resolution;
        public int WallWidth;
    }

    [Serializable]
    public struct ItemSpawnSettings
    {
        public int MinCooldownSeconds;
        public int MaxCooldownSeconds;
        public int MinDistanceToPlayers;
    }

    [Serializable]
    public struct DangerZoneSettings
    {
        public int StartDelaySeconds;
        public int DurationSeconds;
        public int DamagePerSecond;
    }
}
