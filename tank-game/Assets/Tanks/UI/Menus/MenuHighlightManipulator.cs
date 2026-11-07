using Tanks.UI.Menus.Settings.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public interface IMenuHighlightTarget
    {
        void SetHighlighted(bool highlighted);
    }

    public sealed class MenuHighlightManipulator : Manipulator
    {
        private const string HighlightClass = "menu-highlighted";

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
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
            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            UpdateHighlight(evt.target as VisualElement);
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            UpdateHighlight(evt.target as VisualElement);
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            UpdateHighlight(evt.target as VisualElement);
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            UpdateHighlight(evt.target as VisualElement);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            ClearHighlight(target);
            target.Query(className: HighlightClass).ForEach(ClearHighlight);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            DisableScrollbarFocus();
            ConfigureTextFields();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            DisableScrollbarFocus();
        }

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
                if (highlighted != control) ClearHighlight(highlighted);
            });

            if (control != null && control.enabledInHierarchy)
            {
                bool wasHighlighted = control.ClassListContains(HighlightClass);
                control.AddToClassList(HighlightClass);

                if (wasHighlighted == false && control is IMenuHighlightTarget highlightTarget)
                {
                    highlightTarget.SetHighlighted(true);
                }

                control
                    .GetFirstAncestorOfType<SettingElementBase>()?
                    .AddToClassList(HighlightClass);
            }
        }

        private static void ClearHighlight(VisualElement element)
        {
            if (element.ClassListContains(HighlightClass) == false) return;
            element.RemoveFromClassList(HighlightClass);

            if (element is not IMenuHighlightTarget highlightTarget) return;
            highlightTarget.SetHighlighted(false);
        }

        private static VisualElement GetControl(VisualElement element)
        {
            if (element is Scroller ||
                element?.GetFirstAncestorOfType<Scroller>() != null) return null;

            while (element != null)
            {
                if (element is Button ||
                    element is DropdownField ||
                    element is TextField ||
                    element is IMenuHighlightTarget)
                {
                    return element;
                }

                if ((element is SliderInt ||
                    element is Slider) &&
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
