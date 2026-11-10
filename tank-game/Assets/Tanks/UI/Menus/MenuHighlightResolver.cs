using Tanks.UI.Menus.Settings.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public static class MenuHighlightResolver
    {
        public static VisualElement GetControl(VisualElement element)
        {
            if (element is Scroller || element?.GetFirstAncestorOfType<Scroller>() != null)
            {
                return null;
            }

            while (element != null)
            {
                if (element is Button or DropdownField or TextField or IMenuHighlightTarget)
                {
                    return element;
                }

                if (element is SliderInt or Slider &&
                    element.GetFirstAncestorOfType<SliderSetting>() != null)
                {
                    return element;
                }

                element = element.parent;
            }

            return null;
        }
    }
}
