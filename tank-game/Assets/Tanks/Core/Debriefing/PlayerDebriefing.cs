using Tanks.Core.Players;

namespace Tanks.Core.Debriefing
{
    public sealed class PlayerDebriefing
    {
        public string MatchId;
        public string PlayerId;
        public bool IsVictory;
        public StatDebriefing[] Rows;
        public PlayerLevel BeforeLevel;
        public PlayerLevel AfterLevel;
        public PlayerBalance BeforeBalance;
        public PlayerBalance AfterBalance;
    }
}
