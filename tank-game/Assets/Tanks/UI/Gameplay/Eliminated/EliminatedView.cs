using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedView : ViewBase<EliminatedViewModel>
    {
        private VisualElement _container;
        private Label _title;
        private Label _subtitle;

        protected override VisualElement GetRootElement() => _container;

        protected override void Initialize(VisualElement root)
        {
            _container = root.Q("EliminatedContainer");
            _title = _container.Q<Label>("EliminatedText");
            _subtitle = _container.Q<Label>("EliminatedSubtitle");
        }

        protected override void Bind(EliminatedViewModel viewModel)
        {
            viewModel.Title.BindToTarget(x =>
            {
                _title.text = x;

                if (string.IsNullOrEmpty(x)) return;

                LMotion.Create(from: 1f, to: 0f, duration: 1.25f)
                    .WithDelay(1.2f)
                    .WithEase(Ease.OutSine)
                    .Bind(x => _container.style.opacity = x);
            });

            viewModel.Subtitle.BindToTarget(x => _subtitle.text = x);
        }
    }
}
