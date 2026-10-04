using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    [UxmlElement]
    public partial class SettingDropdown : SettingElementBase
    {
        public SettingDropdown()
        {
            _dropdown = new DropdownField(Label);
            ChoiceKeys = new List<string>();
            Value = 0;

            Add(_dropdown);
            _dropdown.RegisterValueChangedCallback(x => Value = _dropdown.index);
            
        }

        [AutoStaticsCleanup]
        private static readonly StringTable _table =
            LocalizationSettings.StringDatabase.GetTable("Menus");

        [UxmlAttribute]
        public List<string> ChoiceKeys
        {
            get => _choiceKeys;
            set
            {
                _choiceKeys = value ?? new List<string>();

                _dropdown.choices = _choiceKeys
                    .Select(x => _table.GetEntry(x)?.Value ?? x)
                    .ToList();
            }
        }

        private readonly DropdownField _dropdown;
        private List<string> _choiceKeys;

        protected override void OnLabelChanged(string newLabel)
        {
            _dropdown.label = newLabel;
            base.OnLabelChanged(newLabel);
        }

        protected override void OnValueChanged(int newValue)
        {
            if (newValue >= 0 && newValue < _dropdown.choices.Count)
            {
                _dropdown.index = newValue;
            }

            base.OnValueChanged(newValue);
        }
    }
}
