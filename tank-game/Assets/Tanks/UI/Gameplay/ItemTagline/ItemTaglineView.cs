using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.ItemTagline
{
    public sealed class ItemTaglineView : ViewBase<ItemTaglineViewModel>
    {
        private VisualElement _root;
        private VisualElement _background;
        private VisualElement _icon;
        private Label _header;
        private Label _tagline;
        private MotionHandle _opacityMotion;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("ItemTagline");
            _background = _root.Q("Background");
            _icon = _root.Q("Icon");
            _header = _root.Q<Label>("Header");
            _tagline = _root.Q<Label>("Tagline");
            _background.style.opacity = 0;
        }

        protected override void Bind(ItemTaglineViewModel viewModel)
        {
            viewModel.Icon.Bind(x => _icon.style.backgroundImage = new StyleBackground(x));

            viewModel.AccentColor.Bind(x =>
            {
                _background.style.borderTopColor = x;
                _background.style.borderRightColor = x;
                _background.style.borderBottomColor = x;
                _background.style.borderLeftColor = x;
            });

            viewModel.Header.Bind(x => _header.text = x);
            viewModel.Tagline.Bind(x =>
            {
                if (string.IsNullOrWhiteSpace(x)) return;

                _opacityMotion.TryCancel();
                _opacityMotion = LMotion.Create(1f, 0f, duration: 4)
                    .WithEase(Ease.InExpo)
                    .Bind(x => _background.style.opacity = x);

                _tagline.text = x;
            });
        }
    }
}
