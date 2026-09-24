using Cysharp.Threading.Tasks;
using Tanks.Core.Common;

namespace Tanks.Core
{
    public interface IAuthService
    {
        PlayerProfileState CurrentProfile { get; }

        UniTask InitializeAsync();
        UniTask<ResultType> SetupProfileAsync(string displayName);

        UniTask<ResultType<string>> GenerateSignInCodeAsync(string identifier);
        UniTask<ResultType> ConfirmCodeAsync(string code);

        UniTask<ResultType<PlayerProfileResult>> SignInWithCodeAsync();
        UniTask<ResultType<PlayerProfileResult>> SignInWithDeviceIdAsync();
    }
}
