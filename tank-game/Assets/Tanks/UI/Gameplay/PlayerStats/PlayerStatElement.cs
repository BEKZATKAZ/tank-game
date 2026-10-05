using LitMotion;
using System.Globalization;
using Tanks.Shared.Stats;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PlayerStats
{
    [UxmlElement]
    public sealed partial class PlayerStatElement : VisualElement
    {
        public PlayerStatElement()
        {
            _icon = new VisualElement { name = "Icon" };
            _valueText = new Label() { name = "Value" };
            _changeText = new Label() { name = "Change" };

            _icon.AddToClassList("stat-icon");
            _valueText.AddToClassList("stat-value");
            _changeText.AddToClassList("stat-change");

            Add(_icon);
            Add(_valueText);
            Add(_changeText);
            AddToClassList("stat");

            Value = 1;
            Change = 0.5f;
        }

        [UxmlAttribute]
        public StatType Type { get; set; }

        [UxmlAttribute]
        [CreateProperty]
        public Sprite Icon
        {
            get => _icon.style.backgroundImage.value.sprite;
            set
            {
                _icon.style.backgroundImage = new StyleBackground(value);
                NotifyPropertyChanged(nameof(Icon));
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public float Value
        {
            get => _value;
            set
            {
                if (_value == value) return;

                _value = value;
                _valueText.text = _value.ToString("F2", CultureInfo.InvariantCulture);
                NotifyPropertyChanged(nameof(Value));
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public float Change
        {
            get => _change;
            set
            {
                if (value == 0) return;

                _change = value;
                _changeText.text = _change.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture);
                _changeText.style.color = _change >= 0 ? Color.green : Color.red;

                _changeMotion.TryCancel();
                _changeMotion = LMotion.Create(0.8f, 0, duration: 3)
                    .WithEase(Ease.InExpo)
                    .Bind(x => _changeText.style.opacity = x);

                NotifyPropertyChanged(nameof(Change));
            }
        }

        private readonly VisualElement _icon;
        private readonly Label _valueText;
        private readonly Label _changeText;

        private float _value;
        private float _change;
        private MotionHandle _changeMotion;
    }
}
