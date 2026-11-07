using Cysharp.Threading.Tasks;
using System;
using Tanks.Core;
using Tanks.Core.Common;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using VContainer;

namespace Tanks.Infrastructure
{
    public sealed class UGSAuthService : IAuthService
    {
        [Inject]
        public UGSAuthService(EnvironmentSettings environment)
            : this(environment, options => UnityServices.InitializeAsync(options).AsUniTask()) { }

        public UGSAuthService(EnvironmentSettings environment, Func<InitializationOptions, UniTask> initialize)
        {
            _environment = environment;
            _initialize = initialize;
        }

        public PlayerProfileState CurrentProfile { get; private set; } = new();

        private readonly EnvironmentSettings _environment;
        private readonly Func<InitializationOptions, UniTask> _initialize;

        private IAuthenticationService Service => AuthenticationService.Instance;

        public UniTask InitializeAsync()
        {
            return _initialize(new InitializationOptions().SetEnvironmentName(_environment.EnvironmentName));
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
            string displayName = Service.PlayerName?.Split('#')[0] ?? "";

            CurrentProfile.UserId.Value = Service.PlayerId;
            CurrentProfile.DisplayName.Value = displayName;

            return new PlayerProfileResult
            {
                UserId = Service.PlayerId,
                DisplayName = displayName
            };
        }
    }
}
