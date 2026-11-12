using System.Threading.Tasks;

namespace Tanks.Core.Debriefing
{
    public interface IDebriefingService
    {
        Task<ResultType<DebriefingResponse>> SubmitAsync(
            string matchId,
            PlayerDebriefingPayload[] players);
    }
}
