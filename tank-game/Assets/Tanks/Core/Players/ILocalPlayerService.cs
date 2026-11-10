using System.Threading.Tasks;

namespace Tanks.Core.Players
{
    public interface ILocalPlayerService
    {
        LocalPlayer LocalPlayer { get; }
        bool IsLoaded { get; }

        Task<ResultType> LoadDataAsync();
        Task<ResultType> ClaimRewardAsync(TimedRewardType type);
    }
}
