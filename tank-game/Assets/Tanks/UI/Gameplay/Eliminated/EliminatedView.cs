using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedView : ViewBase<EliminatedViewModel>
    {
        private VisualElement _container;
        private Label _subtitle;

        protected override VisualElement GetRootElement() => _container;

        protected override void Initialize(VisualElement root)
        {
            _container = root.Q("EliminatedContainer");
            _subtitle = _container.Q<Label>("EliminatedSubtitle");
            _container.style.opacity = 0;
        }

        protected override void Bind(EliminatedViewModel viewModel)
        {
            viewModel.EliminatedBy.Bind(x =>
            {
                _subtitle.text = x;

                // Prevent animation at the start of the game
                if (string.IsNullOrEmpty(x)) return;

                _container.style.opacity = 1;

                LMotion.Create(from: 1f, to: 0f, duration: 1.25f)
                    .WithDelay(1.2f)
                    .WithEase(Ease.OutSine)
                    .Bind(x => _container.style.opacity = x);
            });
        }
    }
}
