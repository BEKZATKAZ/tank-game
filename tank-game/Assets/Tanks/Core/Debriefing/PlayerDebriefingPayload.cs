namespace Tanks.Core.Debriefing
{
    public struct PlayerDebriefingPayload
    {
        public string PlayerId;
        public int Eliminations;
        public int Assists;
        public int Deaths;
        public int Score;
        public bool IsVictory;
    }
}
