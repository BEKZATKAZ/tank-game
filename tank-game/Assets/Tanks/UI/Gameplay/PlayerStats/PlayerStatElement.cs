using LitMotion;
using System.Collections.Generic;
using System.Globalization;
using Tanks.Shared.Stats;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
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
            _timers = new VisualElement { name = "Timers", pickingMode = PickingMode.Ignore };

            _icon.AddToClassList("stat-icon");
            _valueText.AddToClassList("stat-value");
            _changeText.AddToClassList("stat-change");
            _timers.AddToClassList("stat-timers");
            _timers.style.display = DisplayStyle.None;

            Add(_icon);
            Add(_valueText);
            Add(_timers);
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

        public void UpdateTimers(IReadOnlyList<StatTimer> timers)
        {
            while (_timerLabels.Count < timers.Count)
            {
                Label label = new() { pickingMode = PickingMode.Ignore };
                IntVariable seconds = new();
                LocalizedString duration = new("Stats", "stat.duration-seconds")
                {
                    { "seconds", seconds }
                };

                label.SetBinding(nameof(Label.text), duration);
                label.AddToClassList("stat-timer");

                _timerLabels.Add((label, seconds));
                _timers.Add(label);
            }

            for (int i = 0; i < _timerLabels.Count; i++)
            {
                (Label label, IntVariable seconds) = _timerLabels[i];
                bool isVisible = i < timers.Count;

                label.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

                if (isVisible == false) continue;

                seconds.Value = Mathf.CeilToInt(timers[i].RemainingSeconds);
            }

            _timers.style.display = timers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            EnableInClassList("stat--temporary", timers.Count > 0);
        }

        private readonly VisualElement _icon;
        private readonly Label _valueText;
        private readonly Label _changeText;
        private readonly VisualElement _timers;
        private readonly List<(Label Label, IntVariable Seconds)> _timerLabels = new();

        private float _value;
        private float _change;
        private MotionHandle _changeMotion;
    }
}
