using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PickupFeed
{
    [UxmlElement]
    public sealed partial class PickupFeedElement : VisualElement
    {
        public PickupFeedElement()
        {
            _text = new Label
            {
                name = "Text",
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            _icon = new VisualElement
            {
                name = "Icon",
                pickingMode = PickingMode.Ignore
            };

            _text.AddToClassList("pickup-feed__text");
            _icon.AddToClassList("pickup-feed__icon");

            Add(_text);
            Add(_icon);

            AddToClassList("killfeed__entry");
            AddToClassList("pickup-feed__entry");

            pickingMode = PickingMode.Ignore;
            RegisterCallback<DetachFromPanelEvent>(_ => CancelAnimations());
        }

        private readonly Label _text;
        private readonly VisualElement _icon;
        private MotionHandle _slideMotion;
        private MotionHandle _fadeMotion;

        public void Set(PickupFeedEntry entry)
        {
            _text.text = $"{entry.Player} {entry.PickedUp}";
            _icon.style.backgroundImage = new StyleBackground(entry.Icon);

            style.borderTopColor = entry.AccentColor;
            style.borderRightColor = entry.AccentColor;
            style.borderBottomColor = entry.AccentColor;
            style.borderLeftColor = entry.AccentColor;
        }

        public void Play()
        {
            CancelAnimations();

            style.opacity = 1;
            style.visibility = Visibility.Hidden;
            RegisterCallback<GeometryChangedEvent>(SlideIn);

            _fadeMotion = LMotion.Create(1f, 0f, duration: 0.75f)
                .WithDelay(4.25f)
                .Bind(x => style.opacity = x);
        }

        private void SlideIn(GeometryChangedEvent evt)
        {
            if (evt.newRect.width <= 0 || panel is null) return;

            UnregisterCallback<GeometryChangedEvent>(SlideIn);

            float offset = panel.visualTree.worldBound.xMax - worldBound.xMin + 1;
            style.translate = new Translate(offset, 0);
            style.visibility = Visibility.Visible;

            _slideMotion = LMotion.Create(offset, 0f, duration: 0.3f)
                .WithEase(Ease.OutBack)
                .Bind(x => style.translate = new Translate(x, 0));
        }

        private void CancelAnimations()
        {
            UnregisterCallback<GeometryChangedEvent>(SlideIn);
            _slideMotion.TryCancel();
            _fadeMotion.TryCancel();
        }
    }
}
