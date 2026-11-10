using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    public static class ElementSpriteExtensions
    {
        private static readonly ConditionalWeakTable<VisualElement,
            ElementSpriteColorManipulator> _colors = new();

        public static void SetSpriteBorderColor(this VisualElement element, Color color)
        {
            element.SetSpriteColors(color, null);
        }

        public static void SetSpriteColors(
            this VisualElement element,
            Color border,
            Color? fill,
            bool revealWhenReady = false)
        {
            _colors.GetValue(element, CreateColors).SetColors(border, fill, revealWhenReady);
        }

        private static ElementSpriteColorManipulator CreateColors(VisualElement element)
        {
            ElementSpriteColorManipulator colors = new();
            element.AddManipulator(colors);
            return colors;
        }
    }
}
