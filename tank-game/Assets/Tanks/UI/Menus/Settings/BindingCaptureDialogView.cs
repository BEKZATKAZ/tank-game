using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Settings.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings
{
    public sealed class BindingCaptureDialogView
    {
        private VisualElement _overlay;
        private VisualElement _settings;
        private Label _prompt;
        private BindingCaptureDialogViewModel _viewModel;
        private DialogFocusScope _focus;
        private bool _visible;
        private int _closedFrame = -1;

        public bool IsBlockingNavigation =>
            _visible || _closedFrame == Time.frameCount;

        public void Initialize(VisualElement root)
        {
            _overlay = root.Q("BindingCaptureOverlay");
            _settings = root.Q("Settings");
            _prompt = root.Q<Label>("BindingCapturePrompt");
            _overlay.focusable = true;
            _overlay.RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        public void Bind(BindingCaptureDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.IsVisible.Bind(SetVisible);
        }

        public void Unbind()
        {
            if (_viewModel == null) return;

            _viewModel.Cancel();
            _viewModel.IsVisible.Unbind(SetVisible);
            SetVisible(false);
            _viewModel = null;
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible) return;

            _visible = visible;
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                BindingLabels.Bind(_prompt, "text",
                    _viewModel.IsGamepad ? "capture-gamepad" : "capture-keyboard");

                if (_overlay.panel != null)
                {
                    _focus = new DialogFocusScope(_overlay);
                    _focus.Focus(_overlay);
                }

                _settings.SetEnabled(false);
                return;
            }

            _closedFrame = Time.frameCount;
            _settings.SetEnabled(true);
            _focus?.Dispose();
            _focus = null;
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            if (evt.target == _overlay) _viewModel?.Cancel();
        }
    }
}
