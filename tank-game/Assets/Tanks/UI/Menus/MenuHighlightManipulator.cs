using UnityEngine.UIElements;

namespace Tanks.UI.Menus
{
    public sealed class MenuHighlightManipulator : Manipulator
    {
        private readonly MenuControlSetupManipulator _setup = new();

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddManipulator(_setup);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove,
                TrickleDown.TrickleDown);

            target.RegisterCallback<PointerOverEvent>(OnPointerOver,
                TrickleDown.TrickleDown);

            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter,
                TrickleDown.TrickleDown);

            target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.RemoveManipulator(_setup);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove,
                TrickleDown.TrickleDown);

            target.UnregisterCallback<PointerOverEvent>(OnPointerOver,
                TrickleDown.TrickleDown);

            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter,
                TrickleDown.TrickleDown);

            target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            MenuHighlightStateService.Get(target.panel)?.ClearWithin(target);
        }

        private void OnPointerMove(PointerMoveEvent value) => Update(value.target);
        private void OnPointerOver(PointerOverEvent value) => Update(value.target);
        private void OnPointerEnter(PointerEnterEvent value) => Update(value.target);
        private void OnFocusIn(FocusInEvent value) => Update(value.target);

        private void OnPointerLeave(PointerLeaveEvent value)
        {
            MenuHighlightStateService.Get(target.panel)?.ClearWithin(target);
        }

        private void OnDetach(DetachFromPanelEvent value)
        {
            MenuHighlightStateService.Get(value.originPanel)?.ClearWithin(target);
        }

        private void Update(IEventHandler source)
        {
            VisualElement control = MenuHighlightResolver.GetControl(source as
                VisualElement);

            MenuHighlightStateService.Get(target.panel)?.Set(control);
        }
    }
}
