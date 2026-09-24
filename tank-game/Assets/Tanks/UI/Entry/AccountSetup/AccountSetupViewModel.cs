using Cysharp.Threading.Tasks;
using Tanks.Core;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Entry.AccountSetup
{
    public sealed class AccountSetupViewModel : IViewModel
    {
        public AccountSetupViewModel(IAuthService service, IDialogScreenService dialog)
        {
            _service = service;
            _dialog = dialog;
        }

        private readonly IAuthService _service;
        private readonly IDialogScreenService _dialog;

        public BindableProperty<string> DisplayName { get; } = new("");
        public BindableProperty<bool> IsLoading { get; } = new();
        public BindableProperty<bool> IsChoosingDisplayName { get; } = new();

        public async UniTask<bool> LoginAsync()
        {
            await _service.InitializeAsync();

            var result = await _service.SignInWithDeviceIdAsync();

            if (result.IsSuccess == false)
            {
                _dialog.ShowErrorFromCode("error.entry", result.ErrorCode);
                return false;
            }

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
            IsLoading.Value = true;

            var result = await _service.SetupProfileAsync(DisplayName.Value);
            IsLoading.Value = false;

            if (result.IsSuccess == false)
            {
                _dialog.ShowErrorFromCode("error.entry", result.ErrorCode);
            }

            return result.IsSuccess;
        }
    }
}
