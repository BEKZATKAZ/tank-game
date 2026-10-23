using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Killfeed
{
    [UxmlElement]
    public sealed partial class KillfeedElement : VisualElement
    {
        public KillfeedElement()
        {
            _from = new Label
            {
                name = "From",
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            _target = new Label
            {
                name = "Target",
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            VisualElement skull = new VisualElement
            {
                name = "Skull",
                pickingMode = PickingMode.Ignore
            };

            _target.AddToClassList("killfeed__name");
            _from.AddToClassList("killfeed__name");
            skull.AddToClassList("killfeed__skull");

            Add(_from);
            Add(skull);
            Add(_target);
            AddToClassList("killfeed__entry");
            pickingMode = PickingMode.Ignore;
            RegisterCallback<DetachFromPanelEvent>(_ => CancelAnimations());
        }

        private readonly Label _from;
        private readonly Label _target;
        private MotionHandle _slideMotion;
        private MotionHandle _fadeMotion;

        public void Set(KillfeedEntry entry)
        {
            _from.text = entry.From;
            _target.text = entry.Target;
            _target.style.display = entry.Target is null ? DisplayStyle.None : DisplayStyle.Flex;
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
