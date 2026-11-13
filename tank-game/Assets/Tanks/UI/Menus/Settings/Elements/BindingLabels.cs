using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    public static class BindingLabels
    {
        public static void Bind(VisualElement element, string property, string key)
        {
            element.SetBinding(property, new LocalizedString(
                "Menus", "menu.settings." + key));
        }

        public static LocalizedString Display(string path)
        {
            if (string.IsNullOrEmpty(path))
                return new LocalizedString("Menus", "menu.settings.unassigned");

            string control = path[(path.IndexOf('/') + 1)..];
            string key = "menu.settings.input." + control;
            LocalizedString result = new("Menus", key);

            // Device layouts can expose names outside the translated catalog.
            if (KnownControl(control) == false)
            {
                result = new LocalizedString("Menus", "menu.settings.binding-name");
                result["value"] = new StringVariable
                {
                    Value = InputControlPath.ToHumanReadableString(path,
                        InputControlPath.HumanReadableStringOptions.OmitDevice)
                };
            }

            return result;
        }

        private static bool KnownControl(string control)
        {
            return BindingDisplayKeys.Additional.Contains(control) ||
                control.Length == 1 || control is "space" or "leftShift" or
                "rightShift" or "upArrow" or "downArrow" or "leftArrow" or
                "rightArrow" or "leftButton" or "rightButton" or "middleButton" or
                "leftTrigger" or "rightTrigger" or "leftShoulder" or
                "rightShoulder" || control.StartsWith("leftStick/",
                    StringComparison.Ordinal) || control.StartsWith("dpad/",
                    StringComparison.Ordinal);
        }
    }
}
