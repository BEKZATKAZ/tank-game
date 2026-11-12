using System.Threading.Tasks;
using Tanks.Core.Debriefing;

namespace Tanks.Server.Debriefing
{
    public interface IMatchDebriefingSubmissionService
    {
        Task<DebriefingPlayerResult[]> SubmitAsync(
            string matchId,
            PlayerDebriefingPayload[] players,
            float timeout);
    }
}
