using Backend.Models.Debriefing;
using Backend.Models.Economy;
using Backend.Models.Leveling;

namespace Backend.Services;

public interface IDebriefingCalculationService
{
    PlayerDebriefing Calculate(
        string matchId,
        PlayerDebriefingPayload player,
        Level level,
        PlayerBalance balance);
}
