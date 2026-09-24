using Cysharp.Threading.Tasks;
using System;
using Tanks.Core;
using Tanks.Core.Common;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Tanks.Infrastructure
{
    public sealed class UGSAuthService : IAuthService
    {
        public PlayerProfileState CurrentProfile { get; private set; } = new();

        private IAuthenticationService Service => AuthenticationService.Instance;

        public UniTask InitializeAsync()
        {
            return UnityServices.InitializeAsync().AsUniTask();
        }

        public async UniTask<ResultType<string>> GenerateSignInCodeAsync(string identifier)
        {
            try
            {
                SignInCodeInfo code = await Service.GenerateSignInCodeAsync(identifier);
                return code.SignInCode;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<string>("unknown");
            }
        }

        public async UniTask<ResultType> ConfirmCodeAsync(string code)
        {
            try
            {
                await Service.ConfirmCodeAsync(code);
                return Result.Success();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<string>("unknown");
            }
        }

        public async UniTask<ResultType<PlayerProfileResult>> SignInWithCodeAsync()
        {
            try
            {
                await Service.SignInWithCodeAsync(usePolling: true);
                return PostSignIn();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<PlayerProfileResult>("unknown");
            }
        }

        public async UniTask<ResultType<PlayerProfileResult>> SignInWithDeviceIdAsync()
        {
            try
            {
                await Service.SignInAnonymouslyAsync();
                return PostSignIn();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<PlayerProfileResult>("unknown");
            }
        }

        public async UniTask<ResultType> SetupProfileAsync(string displayName)
        {
            try
            {
                await Service.UpdatePlayerNameAsync(displayName);
                CurrentProfile.DisplayName.Value = displayName;
                return Result.Success();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("unknown");
            }
        }

        private PlayerProfileResult PostSignIn()
        {
            CurrentProfile.UserId.Value = Service.PlayerId;
            CurrentProfile.DisplayName.Value = Service.PlayerName;

            return new PlayerProfileResult
            {
                UserId = Service.PlayerId,
                DisplayName = Service.PlayerName
            };
        }
    }
}
