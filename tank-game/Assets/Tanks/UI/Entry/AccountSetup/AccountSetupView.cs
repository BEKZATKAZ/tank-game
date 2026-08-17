using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tanks.UI.Entry.AccountSetup
{
    public sealed class AccountSetupView : ViewBase<AccountSetupViewModel>
    {
        private VisualElement _root;
        private VisualElement _container;
        private TextField _displayNameField;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _container = root.Q("AccountSetup");

            _displayNameField = _container.Q<TextField>();
            _displayNameField.RegisterValueChangedCallback(OnDisplayNameChanged);

            Button confirm = _container.Q<Button>("ConfirmButton");
            confirm.clicked += ConfirmDisplayName;
        }

        protected override async void Bind(AccountSetupViewModel viewModel)
        {
            viewModel.DisplayName
                .Bind(_displayNameField.SetValueWithoutNotify);

            viewModel.IsLoading
                .Bind(x => _root.enabledSelf = !x);

            viewModel.IsChoosingDisplayName
                .Bind(x => _container.style.display = x ? DisplayStyle.Flex : DisplayStyle.None);

            bool success = await viewModel.LoginAsync();
            if (success) await SceneManager.LoadSceneAsync("Menus").ToUniTask();
        }

        private void OnDisplayNameChanged(ChangeEvent<string> e)
        {
            ViewModel.DisplayName.Value = e.newValue;
        }

        private async void ConfirmDisplayName()
        {
            bool success = await ViewModel.ConfirmAsync();
            if (success) await SceneManager.LoadSceneAsync("Menus").ToUniTask();
        }
    }
}
