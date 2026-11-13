using System;
using Tanks.UI.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    public sealed class ControlBindingRow : VisualElement
    {
        public ControlBindingRow(
            string row,
            Action<string> capture,
            Action<string> clear)
        {
            AddToClassList("control-row");
            Label label = new();

            label.AddToClassList("control-row__label");
            BindingLabels.Bind(label, "text", "control." + row);
            Add(label);

            foreach (string device in new[] { "keyboard", "gamepad" })
            {
                for (int index = 0; index < 2; index++)
                {
                    string slot = $"{row}.{device}.{index}";
                    BindingField field = new(slot, capture);

                    Add(field);
                }
            }

            CustomButton clearButton = new();

            clearButton.clicked += () => clear(row);
            clearButton.AddToClassList("control-row__clear");
            BindingLabels.Bind(clearButton, "text", "clear-row");
            Add(clearButton);
        }
    }
}
