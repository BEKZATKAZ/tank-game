using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Multiplayer;
using Tanks.Core;
using Tanks.Core.Common;
using Unity.Multiplayer.PlayMode;
using UnityEngine;

namespace Tanks.Infrastructure
{
    public sealed class PlayFabAuthService : IAuthService
    {
        public PlayFabAuthService(IPlayFabCaller caller) => _caller = caller;

        public PlayerProfileState CurrentProfile { get; private set; } = new();

        private readonly IPlayFabCaller _caller;

        private void UpdateLocalProfile(PlayerProfileResult state)
        {
            CurrentProfile.UserId.Value = state.UserId;
            CurrentProfile.DisplayName.Value = state.DisplayName;
            Debug.LogFormat("Updated display name '{0}'", state.DisplayName);
        }

        public async UniTask<ResultType> SetupProfileAsync(string displayName)
        {
            displayName = displayName?.Trim() ?? "";

            if (displayName.Length < 3)
            {
                return Result.Failure("too-short-username");
            }

            var result = await _caller.CallAsync<
                UpdateUserTitleDisplayNameRequest, UpdateUserTitleDisplayNameResult>(
                PlayFabClientAPI.UpdateUserTitleDisplayName, new UpdateUserTitleDisplayNameRequest
                {
                    AuthenticationContext = PlayFabSettings.staticPlayer,
                    DisplayName = displayName
                });

            if (result.IsSuccess) CurrentProfile.DisplayName.Value = displayName;
            return result;
        }

        public async UniTask<ResultType<PlayerProfileResult>> GetProfileAsync(string userId)
        {
            var result = await _caller.CallAsync<GetPlayerProfileRequest, GetPlayerProfileResult>(
                PlayFabClientAPI.GetPlayerProfile, new GetPlayerProfileRequest
                {
                    AuthenticationContext = PlayFabSettings.staticPlayer,
                    ProfileConstraints = new PlayerProfileViewConstraints
                    {
                        ShowDisplayName = true
                    }
                });

            var mapped = Result.Map(result, p => new PlayerProfileResult
            {
                UserId = p.PlayerProfile.PlayerId,
                DisplayName = p.PlayerProfile.DisplayName
            });

            if (mapped.IsSuccess && userId == CurrentProfile.UserId.Value)
            {
                UpdateLocalProfile(mapped.Value);
            }

            return mapped;
        }

        public async UniTask<ResultType<PlayerProfileResult>> LoginWithDeviceIdAsync()
        {
            string id = SystemInfo.deviceUniqueIdentifier;

#if UNITY_EDITOR
            // Distinguish Multiplayer Play Mode instances
            // For example 'F7GK9WN2_Player2'
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

            if (result.IsSuccess == false)
            {
                return Result.Failure<PlayerProfileResult>(result.ErrorCode);
            }

            if (PlayFabMultiplayer.IsInitialized == false) PlayFabMultiplayer.Initialize();

            CurrentProfile.UserId.Value = id;

            // Don't fetch profile info if it's new
            if (result.Value.NewlyCreated) return new PlayerProfileResult { UserId = id };

            return await GetProfileAsync(id);
        }
    }
}
