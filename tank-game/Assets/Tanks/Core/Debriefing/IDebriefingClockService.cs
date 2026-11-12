using System.Threading.Tasks;

namespace Tanks.Core.Debriefing
{
    public interface IDebriefingClockService
    {
        float Seconds { get; }
        Task DelayAsync(float seconds);
    }
}
