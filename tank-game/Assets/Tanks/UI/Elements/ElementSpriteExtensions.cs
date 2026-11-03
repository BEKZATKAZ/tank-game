using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    public static class ElementSpriteExtensions
    {
        public static void SetSpriteBorderColor(this VisualElement element, Color color)
        {
            IVisualElementScheduledItem update = null;
            update = element.schedule.Execute(() =>
            {
                MaterialDefinition material = element.resolvedStyle.unityMaterial;
                if (material.IsEmpty()) return;

                material.SetColor("_BorderColor", color);
                element.style.unityMaterial = material;
                update.Pause();
            }).Every(1);
        }
    }
}
