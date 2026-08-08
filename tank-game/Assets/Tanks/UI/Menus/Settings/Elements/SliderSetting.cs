using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    [UxmlElement]
    public partial class SliderSetting : SettingElementBase
    {
        public SliderSetting()
        {
            _slider = new SliderInt(Label, MinValue, MaxValue);
            _slider.RegisterValueChangedCallback(x => Value = x.newValue);

            Value = 42;
            Add(_slider);
        }

        [UxmlAttribute] public int MinValue { get; set; } = 0;
        [UxmlAttribute] public int MaxValue { get; set; } = 100;

        private readonly SliderInt _slider;

        protected override void OnLabelChanged(string newLabel)
        {
            _slider.label = newLabel;
            base.OnLabelChanged(newLabel);
        }

        protected override void OnValueChanged(int newValue)
        {
            _slider.SetValueWithoutNotify(newValue);
            base.OnValueChanged(newValue);
        }
    }
}
