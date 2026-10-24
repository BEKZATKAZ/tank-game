using Tanks.Core.Lobbies.Sessions.DTOs;

namespace Tanks.Core.Lobbies.Sessions
{
    public static class SessionSettingsMapper
    {
        public static SessionSettings FromLobby(
            SessionSettingsFromLobby from,
            ISessionDefaultSettingsConfig defaultConfig)
        {
            DefaultMapGeneration mapGeneration = defaultConfig.MapGeneration;
            DefaultItemSpawn itemSpawn = defaultConfig.ItemSpawn;
            DefaultDangerZone dangerZone = defaultConfig.DangerZone;

            int itemMaxCooldown = defaultConfig.ItemSpawn.CooldownSeconds
                .GetValueFromValue(from.ItemSpawn.Rate);

            return new SessionSettings
            {
                DesiredPlayerCount = defaultConfig.DesiredPlayerCount
                    .CorrectValue(from.PlayerCount),

                MaxScore = defaultConfig.MaxScoreToReach
                    .CorrectValue(from.MaxScore),

                MapGeneration = new MapGenerationSettings
                {
                    Resolution = mapGeneration.Resolution
                        .GetValueFromEnum(from.MapGeneration.Size),

                    WallWidth = mapGeneration.WallWidth
                        .GetValueFromValue(from.MapGeneration.Thickness)
                },

                ItemSpawn = new ItemSpawnSettings
                {
                    MinCooldownSeconds = (int)(itemMaxCooldown * 0.5f),
                    MaxCooldownSeconds = itemMaxCooldown,
                    MinDistanceToPlayers = itemSpawn.MinDistanceToPlayers
                },

                DangerZone = new DangerZoneSettings
                {
                    StartDelaySeconds = dangerZone.StartDelaySeconds
                        .CorrectValue(from.DangerZone.StartDelaySeconds),

                    DurationSeconds = dangerZone.DurationSeconds
                        .CorrectValue(from.DangerZone.DurationSeconds),

                    DamagePerSecond = (uint)dangerZone.DamagePerSecond
                        .CorrectValue(from.DangerZone.DamagePerSecond)
                }
            };
        }

        public static SessionSettingsFromLobby FromDefaultToLobby(
            ISessionDefaultSettingsConfig defaultConfig)
        {
            DefaultMapGeneration mapGeneration = defaultConfig.MapGeneration;
            DefaultItemSpawn itemSpawn = defaultConfig.ItemSpawn;
            DefaultDangerZone dangerZone = defaultConfig.DangerZone;

            return new SessionSettingsFromLobby
            {
                PlayerCount = defaultConfig.DesiredPlayerCount.Default,
                MaxScore = defaultConfig.MaxScoreToReach.Default,

                MapGeneration = new MapGenerationSettingsFromLobby
                {
                    Size = mapGeneration.Resolution.GetDefaultAs<MapSize>(),
                    Thickness = mapGeneration.WallWidth.GetDefaultAs<MapThickness>()
                },

                ItemSpawn = new ItemSpawnSettingsFromLobby
                {
                    Rate = itemSpawn.CooldownSeconds.GetDefaultAs<ItemSpawnRate>()
                },

                DangerZone = new DangerZoneSettingsFromLobby
                {
                    StartDelaySeconds = dangerZone.StartDelaySeconds.Default,
                    DurationSeconds = dangerZone.DurationSeconds.Default,
                    DamagePerSecond = dangerZone.DamagePerSecond.Default
                }
            };
        }
    }
}
