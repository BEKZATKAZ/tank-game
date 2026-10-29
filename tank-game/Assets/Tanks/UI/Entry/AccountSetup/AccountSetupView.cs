using Tanks.UI.Menus;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Entry.AccountSetup
{
    public sealed class AccountSetupView : ViewBase<AccountSetupViewModel>
    {
        private VisualElement _root;
        private VisualElement _container;
        private VisualElement _policyPanel;
        private ScrollView _policyScroll;
        private Label _policyText;
        private VisualElement _policyLoading;
        private VisualElement _policyError;
        private Toggle _consentToggle;
        private Button _continue;
        private Button _retry;
        private TextField _displayNameField;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            root.AddManipulator(new MenuHighlightManipulator());
            _root = root;
            _container = root.Q("AccountSetup");
            _policyPanel = root.Q("PrivacyPolicy");
            _policyScroll = root.Q<ScrollView>("PolicyScroll");
            _policyText = root.Q<Label>("PolicyText");
            _policyLoading = root.Q("PolicyLoading");
            _policyError = root.Q("PolicyError");
            _consentToggle = root.Q<Toggle>("PolicyConsent");
            _continue = root.Q<Button>("PolicyContinue");
            _retry = root.Q<Button>("PolicyRetry");

            _displayNameField = _container.Q<TextField>();
            _displayNameField.RegisterValueChangedCallback(OnDisplayNameChanged);
            _consentToggle.RegisterValueChangedCallback(OnConsentChanged);
            _container.Q<Button>("ConfirmButton").clicked += ConfirmDisplayName;
            _continue.clicked += AcceptPolicy;
            _retry.clicked += Retry;
            root.Q<Button>("PolicyQuit").clicked += Quit;
        }

        protected override async void Bind(AccountSetupViewModel viewModel)
        {
            viewModel.DisplayName.Bind(_displayNameField.SetValueWithoutNotify);
            viewModel.IsLoading.Bind(OnLoadingChanged);
            viewModel.IsChoosingDisplayName.Bind(OnChoosingDisplayNameChanged);
            viewModel.PolicyText.Bind(OnPolicyTextChanged);
            viewModel.IsReviewingPolicy.Bind(OnReviewChanged);
            viewModel.HasPolicyError.Bind(OnPolicyErrorChanged);

            bool success = await viewModel.LoginAsync();
            if (success) viewModel.EnterMainMenu();
        }

        protected override void Unbind(AccountSetupViewModel viewModel)
        {
            viewModel.DisplayName.Unbind(_displayNameField.SetValueWithoutNotify);
            viewModel.IsLoading.Unbind(OnLoadingChanged);
            viewModel.IsChoosingDisplayName.Unbind(OnChoosingDisplayNameChanged);
            viewModel.PolicyText.Unbind(OnPolicyTextChanged);
            viewModel.IsReviewingPolicy.Unbind(OnReviewChanged);
            viewModel.HasPolicyError.Unbind(OnPolicyErrorChanged);
        }

        protected override void OnDestroy()
        {
            if (ViewModel == null) return;

            Unbind(ViewModel);
            ViewModel.Dispose();
        }

        private void OnLoadingChanged(bool loading)
        {
            _container.SetEnabled(!loading);
            _consentToggle.SetEnabled(!loading);
            _retry.SetEnabled(!loading);
            _continue.SetEnabled(!loading && _consentToggle.value);
            _policyLoading.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            UpdatePolicyVisibility();
        }

        private void OnChoosingDisplayNameChanged(bool choosing)
        {
            _container.style.display = choosing ? DisplayStyle.Flex : DisplayStyle.None;
            UpdatePolicyVisibility();
        }

        private void OnPolicyTextChanged(string text)
        {
            _policyText.text = text;
            _policyScroll.scrollOffset = Vector2.zero;
        }

        private void OnReviewChanged(bool reviewing)
        {
            _consentToggle.SetValueWithoutNotify(false);
            _continue.SetEnabled(false);
            _policyScroll.style.display = reviewing ? DisplayStyle.Flex : DisplayStyle.None;
            _consentToggle.style.display = reviewing ? DisplayStyle.Flex : DisplayStyle.None;
            _continue.style.display = reviewing ? DisplayStyle.Flex : DisplayStyle.None;
            UpdatePolicyVisibility();
        }

        private void OnPolicyErrorChanged(bool error)
        {
            _policyError.style.display = error ? DisplayStyle.Flex : DisplayStyle.None;
            _retry.style.display = error ? DisplayStyle.Flex : DisplayStyle.None;
            UpdatePolicyVisibility();
        }

        private void UpdatePolicyVisibility()
        {
            bool visible = ViewModel.IsLoading.Value == false &&
                ViewModel.IsChoosingDisplayName.Value == false &&
                (ViewModel.IsReviewingPolicy.Value || ViewModel.HasPolicyError.Value);

            _policyPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDisplayNameChanged(ChangeEvent<string> e)
        {
            ViewModel.DisplayName.Value = e.newValue;
        }

        private void OnConsentChanged(ChangeEvent<bool> e)
        {
            _continue.SetEnabled(e.newValue && ViewModel.IsLoading.Value == false);
        }

        private async void AcceptPolicy()
        {
            AccountSetupViewModel viewModel = ViewModel;
            bool success = await viewModel.AcceptPolicyAsync(_consentToggle.value);
            if (success) viewModel.EnterMainMenu();
        }

        private async void Retry()
        {
            AccountSetupViewModel viewModel = ViewModel;
            bool success = await viewModel.LoginAsync();
            if (success) viewModel.EnterMainMenu();
        }

        private void Quit()
        {
            ViewModel.Dispose();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private async void ConfirmDisplayName()
        {
            AccountSetupViewModel viewModel = ViewModel;
            bool success = await viewModel.ConfirmAsync();
            if (success) viewModel.EnterMainMenu();
        }
    }
}
