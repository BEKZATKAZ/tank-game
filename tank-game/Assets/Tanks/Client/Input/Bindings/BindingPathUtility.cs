using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace Tanks.Client.Input.Bindings
{
    public static class BindingPathUtility
    {
        public static bool IsValid(string slot, string path)
        {
            if (path == "") return true;
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (path.Equals("<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
                return false;

            bool gamepad = slot.Contains(".gamepad.");
            int end = path.IndexOf(">/", StringComparison.Ordinal);
            if (path[0] != '<' || end < 2) return false;

            string layout = path[1..end];
            string controlPath = path[(end + 2)..];
            if (controlPath.Length == 0) return false;
            if (controlPath.Split('/').Length > 2) return false;
            foreach (char character in controlPath)
            {
                if (char.IsLetterOrDigit(character) == false &&
                    character != '/' && character != '_') return false;
            }

            if (gamepad && layout != "Gamepad") return false;
            if (gamepad == false && layout != "Keyboard" && layout != "Mouse")
                return false;

            InputControl control = InputSystem.FindControl(path);
            if (control != null) return IsAssignable(control, gamepad);

            // Saved paths must also load before a device connects.
            InputControlLayout layoutData = InputSystem.LoadLayout(layout);
            if (layoutData == null) return false;

            string first = controlPath.Split('/')[0];
            foreach (InputControlLayout.ControlItem item in layoutData.controls)
            {
                if (item.name.ToString().Equals(
                    first,
                    StringComparison.OrdinalIgnoreCase) == false) continue;

                string type = item.layout.ToString();
                if (controlPath.Contains('/'))
                    return (type == "Stick" || type == "Dpad") &&
                        IsDirection(controlPath.Split('/')[1]);

                return type == "Button" || type == "Key" || type == "DiscreteButton";
            }

            return false;
        }

        public static bool IsAssignable(InputControl control, bool gamepad)
        {
            if (gamepad ? control.device is not Gamepad :
                control.device is not Keyboard && control.device is not Mouse)
                return false;
            if (control is not ButtonControl ||
                (control.synthetic && control.parent == control.device))
                return false;
            if (control.device is Keyboard keyboard &&
                control == keyboard.escapeKey)
                return false;
            if (control.parent is StickControl || control.parent is DpadControl)
                return IsDirection(control.name);

            return control.parent == control.device;
        }

        public static string CanonicalPath(InputControl control)
        {
            string layout = control.device switch
            {
                Keyboard => "Keyboard",
                Mouse => "Mouse",
                _ => "Gamepad"
            };

            return $"<{layout}>/{control.path[(control.device.path.Length + 1)..]}";
        }

        private static bool IsDirection(string name)
        {
            return name is "up" or "down" or "left" or "right";
        }
    }
}
