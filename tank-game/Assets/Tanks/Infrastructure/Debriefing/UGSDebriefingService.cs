using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Core.Store;

namespace Tanks.Infrastructure.Debriefing
{
    public sealed class UGSDebriefingService : IDebriefingService
    {
        public UGSDebriefingService(ICloudCodeCaller caller) => _caller = caller;

        private readonly ICloudCodeCaller _caller;

        public Task<ResultType<DebriefingResponse>> SubmitAsync(
            string matchId,
            PlayerDebriefingPayload[] players)
        {
            return _caller.CallAsync<GetDebriefing, DebriefingResponse>(
                new GetDebriefing { MatchId = matchId, Players = players });
        }
    }
}
