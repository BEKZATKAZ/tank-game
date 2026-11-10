using LitMotion;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    [UxmlElement]
    public sealed partial class BalanceElement : VisualElement
    {
        public BalanceElement()
        {
            focusable = true;
            AddToClassList("currency-box");
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList("bg-fill");
            Add(_fill);
            _icon = new VisualElement { name = "Icon", pickingMode = PickingMode.Ignore };
            _icon.AddToClassList("currency-icon");
            Add(_icon);
            _counter = new Label("0") { pickingMode = PickingMode.Ignore };
            Add(_counter);

            _tooltip = new VisualElement { pickingMode = PickingMode.Ignore };
            _tooltip.AddToClassList("currency-tooltip");
            _tooltipFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _tooltipFill.AddToClassList("bg-fill");
            _tooltip.Add(_tooltipFill);
            _title = new Label { pickingMode = PickingMode.Ignore };
            _title.AddToClassList("currency-tooltip__title");
            _description = new Label { pickingMode = PickingMode.Ignore };
            _description.AddToClassList("currency-tooltip__description");
            _tooltip.Add(_title);
            _tooltip.Add(_description);
            Add(_tooltip);
            HideTooltip();

            RegisterCallback<PointerEnterEvent>(_ => SetHovered(true));
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(false));
            RegisterCallback<FocusInEvent>(_ => SetFocused(true));
            RegisterCallback<FocusOutEvent>(_ => SetFocused(false));
            RegisterCallback<DetachFromPanelEvent>(_ => ResetPresentation());
            BorderColor = Color.gray;
        }

        private readonly VisualElement _fill;
        private readonly VisualElement _icon;
        private readonly Label _counter;
        private readonly VisualElement _tooltip;
        private readonly VisualElement _tooltipFill;
        private readonly Label _title;
        private readonly Label _description;
        private MotionHandle _motion;
        private Color _border;
        private int _displayed;
        private int _target;
        private bool _hovered;
        private bool _focused;

        [UxmlAttribute]
        public Color BorderColor
        {
            get => _border;
            set
            {
                _border = value;
                _fill.SetSpriteBorderColor(value);
                _tooltipFill.SetSpriteBorderColor(value);
            }
        }

        [UxmlAttribute]
        public Sprite Icon
        {
            get => _icon.style.backgroundImage.value.sprite;
            set => _icon.style.backgroundImage = new StyleBackground(value);
        }

        [UxmlAttribute]
        public string CurrencyName
        {
            get => _title.text;
            set => _title.text = value;
        }

        [UxmlAttribute]
        public string Description
        {
            get => _description.text;
            set => _description.text = value;
        }

        public void SetValue(int value, bool instant)
        {
            if (instant == false && value == _target) return;
            _motion.TryCancel();
            _target = value;

            if (instant || panel == null || _displayed == value)
            {
                SetDisplayedValue(value);
                return;
            }

            _motion = LMotion.Create(_displayed, value, 0.5f)
                .WithEase(Ease.Linear)
                .Bind(SetDisplayedValue);
        }

        public void ResetPresentation()
        {
            SetValue(_target, true);
            _hovered = false;
            _focused = false;
            HideTooltip();
        }

        private void SetDisplayedValue(int value)
        {
            _displayed = value;
            _counter.text = value.ToString("N0");
        }

        private void SetHovered(bool value)
        {
            _hovered = value;
            RefreshTooltip();
        }

        private void SetFocused(bool value)
        {
            _focused = value;
            RefreshTooltip();
        }

        private void RefreshTooltip()
        {
            _tooltip.style.display = _hovered || _focused
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HideTooltip() => _tooltip.style.display = DisplayStyle.None;
    }
}
