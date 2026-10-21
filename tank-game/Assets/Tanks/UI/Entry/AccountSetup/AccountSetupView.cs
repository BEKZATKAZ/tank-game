using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Tanks.UI.Entry.AccountSetup
{
    public sealed class AccountSetupView : ViewBase<AccountSetupViewModel>
    {
        [SerializeField] private LocalizedString _consent;
        [SerializeField] private string _privacyPolicyLink;

        private VisualElement _root;
        private VisualElement _container;
        private Toggle _consentToggle;
        private TextField _displayNameField;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _container = root.Q("AccountSetup");
            _consentToggle = root.Q<Toggle>("Consent");

            _displayNameField = _container.Q<TextField>();
            _displayNameField.RegisterValueChangedCallback(OnDisplayNameChanged);

            Button confirm = _container.Q<Button>("ConfirmButton");
            confirm.clicked += ConfirmDisplayName;

            string consentText = _consent.GetLocalizedString() ?? "";

            consentText = consentText.Replace("[", $"<a href=\"{_privacyPolicyLink}\"><u>");
            consentText = consentText.Replace("]", "</u></a>");

            _consentToggle.label = consentText;
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
            if (success) ViewModel.EnterMainMenu();
        }

        private void OnDisplayNameChanged(ChangeEvent<string> e)
        {
            ViewModel.DisplayName.Value = e.newValue;
        }

        private async void ConfirmDisplayName()
        {
            if (_consentToggle.value == false) return;
            bool success = await ViewModel.ConfirmAsync();
            if (success) ViewModel.EnterMainMenu();
        }
    }
}
