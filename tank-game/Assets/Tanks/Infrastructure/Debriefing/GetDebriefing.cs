using Newtonsoft.Json;
using Tanks.Core.Debriefing;

namespace Tanks.Infrastructure.Debriefing
{
    public struct GetDebriefing
    {
        [JsonProperty("matchId")] public string MatchId;
        [JsonProperty("players")] public PlayerDebriefingPayload[] Players;
    }
}
