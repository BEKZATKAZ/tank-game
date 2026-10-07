using UnityEngine;

namespace Tanks.Utils
{
    public static class ColorExtensions
    {
        public static string ToHexCode(this Color color)
        {
            return ((Color32)color).ToHexCode();
        }

        public static string ToHexCode(this Color32 color)
        {
            string r = color.r.ToString("X").PadLeft(2, '0');
            string g = color.g.ToString("X").PadLeft(2, '0');
            string b = color.b.ToString("X").PadLeft(2, '0');
            string a = "";

            if (color.a < 255) a = color.a.ToString("X").PadLeft(2, '0');

            return r + g + b + a;
        }
    }
}
