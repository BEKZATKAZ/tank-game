using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Multiplayer;
using Tanks.Core;
using Unity.Multiplayer.PlayMode;
using UnityEngine;

namespace Tanks.Infrastructure
{
    public sealed class PlayFabAuthService : IAuthService
    {
        public PlayFabAuthService(IPlayFabCaller caller) => _caller = caller;

        private readonly IPlayFabCaller _caller;

        public async UniTask<ResultType> LoginWithDeviceIdAsync()
        {
            string id = SystemInfo.deviceUniqueIdentifier;

#if UNITY_EDITOR
            // Distinguish Multiplayer Play Mode instances
            if (CurrentPlayer.Tags?.Count > 0)
            {
                id += $"_{CurrentPlayer.Tags[0]}";
            }
#endif

            var result = await _caller.CallAsync<LoginWithCustomIDRequest, LoginResult>(
                PlayFabClientAPI.LoginWithCustomID, new LoginWithCustomIDRequest
                {
                    CustomId = id,
                    CreateAccount = true
                });

            if (result.IsSuccess && PlayFabMultiplayer.IsInitialized == false)
            {
                PlayFabMultiplayer.Initialize();
            }

            return result;
        }
    }
}
