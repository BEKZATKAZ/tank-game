using Cysharp.Threading.Tasks;
using Tanks.Core.Common;

namespace Tanks.Core
{
    public interface IAuthService
    {
        PlayerProfileState CurrentProfile { get; }

        UniTask<ResultType> SetupProfileAsync(string displayName);
        UniTask<ResultType<PlayerProfileResult>> GetProfileAsync(string userId);
        UniTask<ResultType<PlayerProfileResult>> LoginWithDeviceIdAsync();
    }
}
