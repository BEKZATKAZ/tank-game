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
            _root.RegisterCallback<FocusOutEvent>(evt => _root.schedule.Execute(() =>
            {
                if (ViewModel == null || !IsVisible || _root.panel == null) return;
                var focused = _root.panel.focusController.focusedElement as VisualElement;
                if (focused == null || !_root.Contains(focused)) FocusFirstControl();
            }));
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
            _root.RegisterCallback<NavigationCancelEvent>(evt =>
            {
                if (ViewModel == null || !IsVisible) return;
                if (_confirmationDialog.resolvedStyle.display != DisplayStyle.None) OnNoConfirmed();
                else Hide();
                evt.StopPropagation();
            });
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

        protected override void FocusFirstControl()
        {
            if (ViewModel == null || _root.panel == null) return;
            if (_confirmationDialog.resolvedStyle.display != DisplayStyle.None)
            {
                _confirmationDialog.Q<Button>("NoButton").Focus();
            }
            else _errorDialog.Q<Button>().Focus();
        }

        private void OnNoConfirmed()
        {
            DialogScreenViewModel viewModel = ViewModel;
            Hide();
            viewModel.ConfirmationNo();
        }
    }
}
