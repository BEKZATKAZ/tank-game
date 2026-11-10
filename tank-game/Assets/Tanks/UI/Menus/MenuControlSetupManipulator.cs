using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public sealed class MenuControlSetupManipulator : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<AttachToPanelEvent>(OnAttach, TrickleDown.TrickleDown);
            target.Query<TextField>().ForEach(ConfigureTextField);
            target.Query<Scroller>().ForEach(ConfigureScroller);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<AttachToPanelEvent>(OnAttach,
                TrickleDown.TrickleDown);
        }

        private void OnAttach(AttachToPanelEvent value)
        {
            if (value.target is not VisualElement element) return;
            if (element is TextField field) ConfigureTextField(field);
            if (element is Scroller scroller) ConfigureScroller(scroller);
            if (element.GetFirstAncestorOfType<Scroller>() != null) element.focusable =
                false;
        }

        private static void ConfigureTextField(TextField field)
        {
            field.pickingMode = PickingMode.Ignore;
            field.labelElement.pickingMode = PickingMode.Ignore;
            VisualElement input = field.Q(className: TextField.inputUssClassName);
            if (input != null) input.pickingMode = PickingMode.Position;
        }

        private static void ConfigureScroller(Scroller scroller)
        {
            scroller.focusable = false;
            scroller.Query<VisualElement>().ForEach(element => element.focusable = false);
        }
    }
}
