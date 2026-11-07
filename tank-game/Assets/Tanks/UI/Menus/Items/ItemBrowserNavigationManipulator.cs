using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Items
{
    internal sealed class ItemBrowserNavigationManipulator : Manipulator
    {
        public ItemBrowserNavigationManipulator(VisualElement grid, Button back)
        {
            _grid = grid;
            _back = back;
        }

        private readonly VisualElement _grid;
        private readonly Button _back;
        private VisualElement _lastItem;
        private object _lastItemId;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            target.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
        }

        private VisualElement GetItem(VisualElement element)
        {
            while (element != null && element != _grid)
            {
                if (element.parent == _grid) return element;
                element = element.parent;
            }

            return null;
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            VisualElement item = GetItem(evt.target as VisualElement);
            if (item is null) return;

            _lastItem = item;
            _lastItemId = item.userData;
        }

        private VisualElement GetReturnItem()
        {
            if (_lastItem?.parent == _grid) return _lastItem;

            if (_lastItemId != null)
            {
                foreach (VisualElement item in _grid.Children())
                {
                    if (_lastItemId.Equals(item.userData)) return item;
                }
            }

            return _grid.childCount > 0 ? _grid[0] : null;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            VisualElement origin = evt.target as VisualElement;
            VisualElement destination = null;

            if (evt.direction == NavigationMoveEvent.Direction.Left
                && (origin == _back || (origin != null && _back.Contains(origin))))
            {
                destination = GetReturnItem();
            }
            else if (evt.direction is NavigationMoveEvent.Direction.Right or NavigationMoveEvent.Direction.Left)
            {
                VisualElement item = GetItem(origin);
                int index = item is null ? -1 : _grid.IndexOf(item);

                bool exitsRow = evt.direction == NavigationMoveEvent.Direction.Left
                    ? index % 7 == 0
                    : (index + 1) % 7 == 0 || index == _grid.childCount - 1;

                if (index >= 0 && exitsRow)
                {
                    _lastItem = item;
                    _lastItemId = item.userData;
                    destination = _back;
                }
            }

            if (destination is null) return;

            target.focusController?.IgnoreEvent(evt);
            evt.StopImmediatePropagation();
            destination.Focus();
        }
    }
}
