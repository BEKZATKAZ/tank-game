using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Debriefing
{
    public sealed class DebriefingRewardBinding
    {
        private readonly LongVariable _value = new();
        private readonly StringVariable _sign = new();

        public void Bind(Label label, bool xp = false)
        {
            string entry = xp ? "debriefing.xp-reward" : "debriefing.reward";

            LocalizedString text = new("Menus", entry)
            {
                { "value", _value },
                { "sign", _sign }
            };

            label.SetBinding(nameof(Label.text), text);
        }

        public void SetValue(int value, bool negative = false)
        {
            long magnitude = value;

            _sign.Value = value < 0 || negative ? "−" : "+";
            _value.Value = magnitude < 0 ? -magnitude : magnitude;
        }
    }
}
