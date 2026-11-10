using System.Runtime.CompilerServices;
using Tanks.UI.Menus.Settings.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public sealed class MenuHighlightStateService
    {
        private const string HIGHLIGHT_CLASS = "menu-highlighted";
        private static readonly ConditionalWeakTable<IPanel, MenuHighlightStateService>
            _panels
            = new();

        private VisualElement _control;
        private VisualElement _setting;

        public static MenuHighlightStateService Get(IPanel panel)
        {
            return panel == null ? null : _panels.GetOrCreateValue(panel);
        }

        public void Set(VisualElement control)
        {
            if (control != null && control.enabledInHierarchy == false) control = null;
            if (_control == control) return;
            Clear();
            _control = control;
            if (control == null) return;
            control.AddToClassList(HIGHLIGHT_CLASS);

            if (control is IMenuHighlightTarget receiver) receiver.SetHighlighted(true);
            _setting = control.GetFirstAncestorOfType<SettingElementBase>();
            _setting?.AddToClassList(HIGHLIGHT_CLASS);
        }

        public void ClearWithin(VisualElement root)
        {
            if (_control == root || (_control != null && root.Contains(_control))) Clear();
        }

        private void Clear()
        {
            _control?.RemoveFromClassList(HIGHLIGHT_CLASS);
            _setting?.RemoveFromClassList(HIGHLIGHT_CLASS);
            if (_control is IMenuHighlightTarget receiver) receiver.SetHighlighted(false);
            _control = null;
            _setting = null;
        }
    }
}
