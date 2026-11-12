namespace Tanks.Core.Debriefing
{
    public interface IPendingDebriefingService
    {
        string MatchId { get; }
        DebriefingPlayerResult Result { get; }
        bool HasPending { get; }
        void Expect(string matchId);
        void Store(string matchId, DebriefingPlayerResult result);
        void Consume();
    }
}
