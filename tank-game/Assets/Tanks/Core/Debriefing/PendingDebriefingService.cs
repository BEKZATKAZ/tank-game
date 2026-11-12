using System.Collections.Generic;

namespace Tanks.Core.Debriefing
{
    public sealed class PendingDebriefingService : IPendingDebriefingService
    {
        private readonly HashSet<string> _consumed = new();

        public string MatchId { get; private set; }
        public DebriefingPlayerResult Result { get; private set; }
        public bool HasPending => string.IsNullOrEmpty(MatchId) == false;

        public void Expect(string matchId)
        {
            if (string.IsNullOrEmpty(matchId) || _consumed.Contains(matchId)) return;
            if (MatchId == matchId) return;
            MatchId = matchId;
            Result = null;
        }

        public void Store(string matchId, DebriefingPlayerResult result)
        {
            if (string.IsNullOrEmpty(matchId) || _consumed.Contains(matchId)) return;
            if (HasPending && MatchId != matchId) return;
            MatchId = matchId;
            Result = result;
        }

        public void Consume()
        {
            if (HasPending) _consumed.Add(MatchId);
            MatchId = null;
            Result = null;
        }
    }
}
