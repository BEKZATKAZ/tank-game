using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Playground
{
    public sealed class PlaygroundView : ViewBase<PlaygroundViewModel>
    {
        private VisualElement _root;
        private Label _label;
        private MotionHandle _motion;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _label = _root.Q<Label>();
        }

        protected override void Bind(PlaygroundViewModel viewModel)
        {
            viewModel.Prompt.Bind(OnPromptChanged);
        }

        private void OnPromptChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(_label.text) == false)
            {
                _motion.TryCancel();
                _motion = LMotion.Create(1f, 0f, duration: 0.2f)
                    .WithLoops(8, LoopType.Flip)
                    .Bind(x => _label.style.opacity = x);
            }

            _label.text = value;
        }
    }
}
