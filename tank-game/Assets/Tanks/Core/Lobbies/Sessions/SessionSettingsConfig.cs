using NaughtyAttributes;
using System;
using UnityEngine;

namespace Tanks.Core.Lobbies.Sessions
{
    [Serializable]
    public sealed class MapGenerationConfig
    {
        public Vector2Int Resolution = new Vector2Int(10, 6);

        [Range(4, 12)]
        public int WallWidth = 7;
    }

    [Serializable]
    public sealed class ItemSpawnConfig
    {
        [MinMaxSlider(1, 30)]
        public Vector2Int Cooldown = new Vector2Int(4, 8);

        [Range(0, 3)]
        public int MinDistanceToPlayers = 2;
    }

    [Serializable]
    public sealed class DangerZoneConfig
    {
        [Range(5, 30)]
        public int StartDelaySeconds = 15;

        [Range(10, 120)]
        public int DurationSeconds = 40;

        [Range(5, 20)]
        public int DamagePerSecond = 10;
    }

    [CreateAssetMenu(fileName = "SessionSettings", menuName = "Scriptable Objects/Session Settings")]
    public sealed class SessionSettingsConfig : ScriptableObject, ISessionSettings
    {
        [Header("Main")]
        [Range(1, 4)]
        [SerializeField]
        private int _desiredPlayerCount = 1;

        [Header("Other")]
        [SerializeField]
        private MapGenerationConfig _mapGeneration;

        [SerializeField]
        private ItemSpawnConfig _itemSpawn;

        [SerializeField]
        private DangerZoneConfig _dangerZone;

        public int DesiredPlayerCount => _desiredPlayerCount;

        public MapGenerationSettings MapGeneration => new MapGenerationSettings
        {
            Resolution = _mapGeneration.Resolution,
            WallWidth = _mapGeneration.WallWidth
        };

        public ItemSpawnSettings ItemSpawn => new ItemSpawnSettings
        {
            MinCooldownSeconds = _itemSpawn.Cooldown.x,
            MaxCooldownSeconds = _itemSpawn.Cooldown.y,
            MinDistanceToPlayers = _itemSpawn.MinDistanceToPlayers
        };

        public DangerZoneSettings DangerZone => new DangerZoneSettings
        {
            StartDelaySeconds = _dangerZone.StartDelaySeconds,
            DurationSeconds = _dangerZone.DurationSeconds,
            DamagePerSecond = _dangerZone.DamagePerSecond
        };
    }
}
