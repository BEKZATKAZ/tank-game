using Tanks.Core.Lobbies.Sessions.DTOs;

namespace Tanks.Core.Lobbies.Sessions
{
    public static class SessionSettingsMapper
    {
        public static SessionSettings FromLobby(
            SessionSettingsFromLobby from,
            ISessionDefaultSettingsConfig defaultConfig)
        {
            DefaultPlayerStats stats = defaultConfig.Stats;
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

                SelfDamagePercent = defaultConfig.SelfDamagePercent
                    .CorrectValue(from.SelfDamagePercent),

                Stats = new PlayerStatSettings
                {
                    ForwardMovementSpeed = stats.ForwardMovementSpeed
                        .CorrectValue(from.Stats.ForwardMovementSpeed),

                    BackwardsMovementSpeed = stats.BackwardsMovementSpeed
                        .CorrectValue(from.Stats.BackwardsMovementSpeed)
                },

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
            DefaultPlayerStats stats = defaultConfig.Stats;
            DefaultMapGeneration mapGeneration = defaultConfig.MapGeneration;
            DefaultItemSpawn itemSpawn = defaultConfig.ItemSpawn;
            DefaultDangerZone dangerZone = defaultConfig.DangerZone;

            return new SessionSettingsFromLobby
            {
                PlayerCount = defaultConfig.DesiredPlayerCount.Default,
                MaxScore = defaultConfig.MaxScoreToReach.Default,
                SelfDamagePercent = defaultConfig.SelfDamagePercent.Default,

                Stats = new PlayerStatSettingsFromLobby
                {
                    ForwardMovementSpeed = stats.ForwardMovementSpeed.Default,
                    BackwardsMovementSpeed = stats.BackwardsMovementSpeed.Default
                },

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
