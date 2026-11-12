using Backend.Models;
using Backend.Models.Debriefing;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace Backend.Services;

public interface IPlayerDebriefingService
{
    Task<ApiResult<PlayerDebriefing>> CalculateAsync(
        IExecutionContext context,
        string matchId,
        PlayerDebriefingPayload player);
}
