using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Tanks.Core;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Repositories;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Entry.AccountSetup
{
    public sealed class AccountSetupViewModel : IViewModel, IDisposable
    {
        public AccountSetupViewModel(
            IAuthService service,
            IResourceLoader resourceLoader,
            IDialogScreenService dialog,
            ISceneLoader sceneLoader,
            IPrivacyPolicyService privacyPolicy)
        {
            _service = service;
            _resourceLoader = resourceLoader;
            _dialog = dialog;
            _sceneLoader = sceneLoader;
            _privacyPolicy = privacyPolicy;
            _cancellationToken = _lifetime.Token;
        }

        private readonly IAuthService _service;
        private readonly IResourceLoader _resourceLoader;
        private readonly IDialogScreenService _dialog;
        private readonly ISceneLoader _sceneLoader;
        private readonly IPrivacyPolicyService _privacyPolicy;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly CancellationToken _cancellationToken;

        private PrivacyPolicy _policy;
        private bool _authenticated;

        public BindableProperty<string> DisplayName { get; } = new("");
        public BindableProperty<bool> IsLoading { get; } = new();
        public BindableProperty<bool> IsChoosingDisplayName { get; } = new();
        public BindableProperty<string> PolicyText { get; } = new("");
        public BindableProperty<bool> IsReviewingPolicy { get; } = new();
        public BindableProperty<bool> HasPolicyError { get; } = new();

        public void Dispose()
        {
            if (_cancellationToken.IsCancellationRequested) return;

            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        public async UniTask<bool> LoginAsync()
        {
            if (IsLoading.Value || _cancellationToken.IsCancellationRequested) return false;

            IsLoading.Value = true;
            HasPolicyError.Value = false;
            IsReviewingPolicy.Value = false;
            _policy = null;

            try
            {
                _policy = await _privacyPolicy.FetchAsync(_cancellationToken);
                _cancellationToken.ThrowIfCancellationRequested();
                PolicyText.Value = _policy.Text;

                if (_privacyPolicy.IsAccepted(_policy) == false)
                {
                    IsReviewingPolicy.Value = true;
                    return false;
                }

                return await AuthenticateAsync();
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception)
            {
                HasPolicyError.Value = true;
                return false;
            }
            finally
            {
                IsLoading.Value = false;
            }
        }

        public async UniTask<bool> AcceptPolicyAsync(bool agreed)
        {
            if (agreed == false || IsReviewingPolicy.Value == false || IsLoading.Value ||
                _policy == null || _cancellationToken.IsCancellationRequested) return false;

            IsLoading.Value = true;

            try
            {
                _privacyPolicy.Accept(_policy);
                IsReviewingPolicy.Value = false;
                return await AuthenticateAsync();
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception)
            {
                IsReviewingPolicy.Value = false;
                HasPolicyError.Value = true;
                return false;
            }
            finally
            {
                IsLoading.Value = false;
            }
        }

        private async UniTask<bool> AuthenticateAsync()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            await _service.InitializeAsync();
            _cancellationToken.ThrowIfCancellationRequested();

            var result = await _service.SignInWithDeviceIdAsync();
            _cancellationToken.ThrowIfCancellationRequested();

            if (result.IsSuccess == false)
            {
                _dialog.ShowErrorFromCode("error.entry", result.ErrorCode);
                HasPolicyError.Value = true;
                return false;
            }

            _authenticated = true;
            bool isBrandNew = string.IsNullOrWhiteSpace(result.Value.DisplayName);

            if (isBrandNew)
            {
                IsChoosingDisplayName.Value = true;
                return false;
            }

            return true;
        }

        public async UniTask<bool> ConfirmAsync()
        {
            if (_authenticated == false || IsLoading.Value || _cancellationToken.IsCancellationRequested) return false;

            IsLoading.Value = true;

            try
            {
                var result = await _service.SetupProfileAsync(DisplayName.Value);
                _cancellationToken.ThrowIfCancellationRequested();

                if (result.IsSuccess == false)
                {
                    _dialog.ShowErrorFromCode("error.entry", result.ErrorCode);
                }

                return result.IsSuccess;
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception)
            {
                _dialog.ShowErrorFromCode("error.entry", "unknown");
                return false;
            }
            finally
            {
                IsLoading.Value = false;
            }
        }

        public void EnterMainMenu()
        {
            if (_authenticated == false || _cancellationToken.IsCancellationRequested) return;

            _resourceLoader.InitializeAsync().Forget();
            _sceneLoader.StartLoading(new LoadMenus(), withTips: false, loadUntil: 1);
        }
    }
}
