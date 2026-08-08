using System;
using Tanks.Core.Settings;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    [UxmlElement]
    public partial class SettingElementBase : VisualElement
    {
        public SettingElementBase()
        {
            AddToClassList("setting");
        }

        [UxmlAttribute]
        [CreateProperty]
        public PlayerSetting Type { get; set; }

        [UxmlAttribute]
        [CreateProperty]
        public string Label
        {
            get => _label;
            set
            {
                if (_label == value) return;
                _label = value;
                OnLabelChanged(value);
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                if (_withNotify) ValueChanged?.Invoke(Type, value);
                OnValueChanged(value);
            }
        }

        public event Action<PlayerSetting, int> ValueChanged;

        private bool _withNotify = true;
        private string _label;
        private int _value;

        public void SetValueWithoutNotify(int value)
        {
            _withNotify = false;

            try { Value = value; }
            finally { _withNotify = true; }
        }

        protected virtual void OnLabelChanged(string newLabel)
        {
            NotifyPropertyChanged(nameof(Label));
        }

        protected virtual void OnValueChanged(int newValue)
        {
            NotifyPropertyChanged(nameof(Value));
        }
    }
}
