using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Client.Input.Glyphs
{
    public static class InputGlyphResolver
    {
        private static readonly Dictionary<string, string> _names = new(
            StringComparer.OrdinalIgnoreCase)
        {
            { "leftButton", "mouse_left" },
            { "rightButton", "mouse_right" },
            { "middleButton", "mouse_scroll" },
            { "upArrow", "keyboard_arrow_up" },
            { "downArrow", "keyboard_arrow_down" },
            { "leftArrow", "keyboard_arrow_left" },
            { "rightArrow", "keyboard_arrow_right" },
            { "leftShift", "keyboard_shift" },
            { "rightShift", "keyboard_shift" },
            { "leftCtrl", "keyboard_ctrl" },
            { "rightCtrl", "keyboard_ctrl" },
            { "leftAlt", "keyboard_alt" },
            { "rightAlt", "keyboard_alt" },
            { "enter", "keyboard_return" },
            { "tab", "key_tab" },
            { "leftTrigger", "xbox_lt" },
            { "rightTrigger", "xbox_rt" },
            { "leftShoulder", "xbox_lb" },
            { "rightShoulder", "xbox_rb" },
            { "buttonSouth", "xbox_button_a" },
            { "buttonNorth", "xbox_button_y" },
            { "buttonWest", "xbox_button_x" },
            { "buttonEast", "xbox_button_b" }
        };

        public static string KeyFromAssetPath(string assetPath)
        {
            const string PREFIX = "Assets/Tanks/Art/InputGlyphs/";
            string path = assetPath.Replace('\\', '/');
            if (path.StartsWith(PREFIX, StringComparison.Ordinal) == false)
                return null;

            int extension = path.LastIndexOf('.');
            return extension < PREFIX.Length ? null : path[PREFIX.Length..extension];
        }

        public static string GetKey(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            int separator = path.IndexOf("/", StringComparison.Ordinal);
            if (separator < 0) return null;

            string control = path[(separator + 1)..];
            bool gamepad = path.StartsWith("<Gamepad>",
                StringComparison.OrdinalIgnoreCase);
            string folder = gamepad ? "Gamepad/" : "Keyboard & Mouse/";

            if (_names.TryGetValue(control, out string name)) return folder + name;
            if (control.StartsWith("leftStick/"))
                return folder + "xbox_stick_l_" + control[10..];
            if (control.StartsWith("rightStick/"))
                return folder + "xbox_stick_r_" + control[11..];
            if (control.StartsWith("dpad/"))
                return folder + "xbox_dpad_" + control[5..];
            if (path.StartsWith("<Keyboard>"))
                return folder + "keyboard_" + control.ToLowerInvariant();

            return null;
        }

        public static Sprite Resolve(InputGlyphDatabase database, string path)
        {
            return database != null &&
                database.TryGet(GetKey(path), out Sprite sprite)
                ? sprite : null;
        }
    }
}
