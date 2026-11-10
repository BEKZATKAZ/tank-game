using Tanks.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardHighlightManipulator : Manipulator
    {
        public CosmeticCardHighlightManipulator(VisualElement fill)
        {
            _fill = fill;
        }

        private readonly VisualElement _fill;
        private readonly CustomStyleProperty<Color> _textProperty = new("--text-color");
        private Color _text = Color.white;
        private bool _hovered;
        private bool _focused;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerEnterEvent>(OnEnter);
            target.RegisterCallback<PointerLeaveEvent>(OnLeave);
            target.RegisterCallback<FocusInEvent>(OnFocusIn);
            target.RegisterCallback<FocusOutEvent>(OnFocusOut);
            target.RegisterCallback<CustomStyleResolvedEvent>(OnStyle);
            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerEnterEvent>(OnEnter);
            target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn);
            target.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            target.UnregisterCallback<CustomStyleResolvedEvent>(OnStyle);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnEnter(PointerEnterEvent value)
        {
            _hovered = true;
            Apply();
        }

        private void OnLeave(PointerLeaveEvent value)
        {
            _hovered = false;
            Apply();
        }
        private void OnFocusIn(FocusInEvent value)
        {
            _focused = true;
            Apply();
        }

        private void OnFocusOut(FocusOutEvent value)
        {
            VisualElement next = value.relatedTarget as VisualElement;
            _focused = next != null && (next == target || target.Contains(next));
            Apply();
        }

        private void OnStyle(CustomStyleResolvedEvent value)
        {
            if (target.customStyle.TryGetValue(_textProperty, out Color text)) _text = text;
            Apply();
        }

        private void OnAttach(AttachToPanelEvent value)
        {
            Apply();
        }

        private void OnDetach(DetachFromPanelEvent value)
        {
            _hovered = false;
            _focused = false;
            Apply();
        }

        private void Apply()
        {
            Color border = _hovered || _focused ? _text : Color.black;
            border.a = 0.5f;
            _fill.SetSpriteColors(border, new Color(24 / 255f, 24 / 255f,
                24 / 255f, 0.5f), revealWhenReady: true);
        }
    }
}
