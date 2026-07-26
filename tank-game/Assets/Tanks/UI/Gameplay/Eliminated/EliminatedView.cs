using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedView : IView<EliminatedModel>
    {
        private VisualElement _container;
        private Label _reason;

        public void Hide()
        {
            _container.style.display = DisplayStyle.None;
        }

        public void Initialize(VisualElement root)
        {
            _container = root.Q("EliminatedContainer");
            _reason = _container.Q<Label>("EliminatedBy");
            _container.style.display = DisplayStyle.None;
        }

        public void Show(EliminatedModel data)
        {
            _container.style.display = DisplayStyle.Flex;
            _reason.text = ""; // TODO: Add reasons

            LMotion.Create(100f, 0, duration: 1.25f)
                .WithDelay(1.2f)
                .WithEase(Ease.OutSine)
                .WithOnComplete(Hide)
                .Bind(x => _container.style.opacity = x);
        }
    }
}
