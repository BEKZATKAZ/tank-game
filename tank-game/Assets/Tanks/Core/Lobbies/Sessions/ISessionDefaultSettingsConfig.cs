namespace Tanks.Core.Lobbies.Sessions
{
    public interface ISessionDefaultSettingsConfig
    {
        PossibleIntegers DesiredPlayerCount { get; }
        PossibleIntegers MaxScoreToReach { get; }
        PossibleIntegers SelfDamagePercent { get; }
        DefaultMapGeneration MapGeneration { get; }
        DefaultItemSpawn ItemSpawn { get; }
        DefaultDangerZone DangerZone { get; }
    }
}
