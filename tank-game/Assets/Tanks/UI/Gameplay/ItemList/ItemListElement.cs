using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.ItemList
{
    [UxmlElement]
    public sealed partial class ItemListElement : VisualElement
    {
        public ItemListElement()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("item-list");
        }

        public void SetEntries(IReadOnlyList<ItemListEntry> entries)
        {
            Clear();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                ItemListEntry entry = entries[i];
                VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("item-list__icon");
                icon.style.backgroundImage = new StyleBackground(entry.Icon);

                if (entry.Count > 1)
                {
                    Label count = new Label($"x{entry.Count}") { pickingMode = PickingMode.Ignore };
                    count.AddToClassList("item-list__count");
                    icon.Add(count);
                }

                Add(icon);
            }
        }
    }
}
