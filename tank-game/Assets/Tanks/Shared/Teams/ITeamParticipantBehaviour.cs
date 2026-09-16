namespace Tanks.Shared.Teams
{
    public interface ITeamParticipantBehaviour : INetworkBehaviourExtended
    {
        Team BelongsTo { get; }
        PlayerState State { get; }
    }
}
