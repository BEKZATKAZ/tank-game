using Cysharp.Threading.Tasks;

namespace Tanks.Core
{
    public interface IAuthService
    {
        UniTask<ResultType> LoginWithDeviceIdAsync();
    }
}
