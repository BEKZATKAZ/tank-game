using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Infrastructure
{
    public sealed class PlayFabAuthService : IAuthService
    {
        public PlayFabAuthService(IPlayFabCaller caller) => _caller = caller;

        private readonly IPlayFabCaller _caller;

        public async UniTask<ResultType> LoginWithDeviceIdAsync()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };

            return await _caller.CallAsync<LoginWithCustomIDRequest, LoginResult>(
                PlayFabClientAPI.LoginWithCustomID, request);
        }
    }
}
