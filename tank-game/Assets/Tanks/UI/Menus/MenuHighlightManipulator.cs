using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public sealed class MenuHighlightManipulator : Manipulator
    {
        private const string HighlightClass = "menu-highlighted";

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            target.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            DisableScrollbarFocus();
            ConfigureTextFields();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnPointerMove(PointerMoveEvent evt) => UpdateHighlight(evt.target as VisualElement);
        private void OnPointerOver(PointerOverEvent evt) => UpdateHighlight(evt.target as VisualElement);
        private void OnPointerLeave(PointerLeaveEvent evt) => UpdateHighlight(null);
        private void OnFocusIn(FocusInEvent evt) => UpdateHighlight(evt.target as VisualElement);

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            DisableScrollbarFocus();
            ConfigureTextFields();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => DisableScrollbarFocus();

        private void ConfigureTextFields()
        {
            target.Query<TextField>().ForEach(field =>
            {
                field.pickingMode = PickingMode.Ignore;
                field.labelElement.pickingMode = PickingMode.Ignore;
                field.Q(className: TextField.inputUssClassName).pickingMode = PickingMode.Position;
            });
        }

        private void DisableScrollbarFocus()
        {
            target.Query<Scroller>().ForEach(scroller =>
            {
                scroller.focusable = false;
                scroller.Query<VisualElement>().ForEach(element => element.focusable = false);
            });
        }

        private void UpdateHighlight(VisualElement element)
        {
            VisualElement control = GetControl(element);
            VisualElement panelRoot = target.panel?.visualTree;
            if (panelRoot is null) return;

            panelRoot.Query(className: HighlightClass).ForEach(highlighted =>
            {
                if (highlighted != control) highlighted.RemoveFromClassList(HighlightClass);
            });

            if (control != null && control.enabledInHierarchy) control.AddToClassList(HighlightClass);
        }

        private static VisualElement GetControl(VisualElement element)
        {
            if (element is Scroller || element?.GetFirstAncestorOfType<Scroller>() != null) return null;

            while (element != null)
            {
                if (element is Button || element is DropdownField || element is TextField) return element;
                if ((element is SliderInt || element is Slider)
                    && element.GetFirstAncestorOfType<Settings.Elements.SliderSetting>() != null) return element;
                element = element.parent;
            }

            return null;
        }
    }
}
