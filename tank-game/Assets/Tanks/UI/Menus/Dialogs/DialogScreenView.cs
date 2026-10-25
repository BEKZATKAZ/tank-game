using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Dialogs
{
    public sealed class DialogScreenView : ViewBase<DialogScreenViewModel>
    {
        private VisualElement _root;
        private VisualElement _confirmationDialog;
        private VisualElement _errorDialog;

        private Label _confirmationHeader;
        private Label _confirmationMessage;
        private Label _errorMessage;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _errorDialog = _root.Q("Error");
            _errorMessage = _errorDialog.Q<Label>("ErrorMessage");

            _confirmationDialog = _root.Q("Confirmation");
            _confirmationHeader = _confirmationDialog.Q<Label>("ConfirmationHeader");
            _confirmationMessage = _confirmationDialog.Q<Label>("ConfirmationMessage");

            Button confirmYes = _confirmationDialog.Q<Button>("YesButton");
            Button confirmNo = _confirmationDialog.Q<Button>("NoButton");
            Button errorOk = _errorDialog.Q<Button>();

            confirmYes.clicked += OnYesConfirmed;
            confirmNo.clicked += OnNoConfirmed;
            errorOk.clicked += Hide;
        }

        protected override void Bind(DialogScreenViewModel viewModel)
        {
            viewModel.Type.Bind(OnTypeChanged);

            viewModel.Header.Bind(x => _confirmationHeader.text = x);
            viewModel.Message.Bind(x => _confirmationMessage.text = x);
            viewModel.Message.Bind(x => _errorMessage.text = x);

            LMotion.Create(50, 0, 0.25f)
                .WithEase(Ease.OutBack)
                .Bind(x =>
                {
                    _confirmationDialog.style.translate = new Translate(0, x, 0);
                    _errorDialog.style.translate = new Translate(0, x, 0);
                });
        }

        private void OnTypeChanged(DialogType type)
        {
            _confirmationDialog.style.display = type is DialogType.Confirmation
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _errorDialog.style.display = type is DialogType.Error
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void OnYesConfirmed()
        {
            DialogScreenViewModel viewModel = ViewModel;
            Hide();
            viewModel.ConfirmationYes();
        }

        private void OnNoConfirmed()
        {
            DialogScreenViewModel viewModel = ViewModel;
            Hide();
            viewModel.ConfirmationNo();
        }
    }
}
