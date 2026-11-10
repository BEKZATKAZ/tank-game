using System;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCardViewport : IDisposable
    {
        public CosmeticCardViewport(ScrollView scroll, CosmeticCardPool pool)
        {
            _scroll = scroll;
            _pool = pool;
        }

        private readonly ScrollView _scroll;
        private readonly CosmeticCardPool _pool;
        private IVisualElementScheduledItem _pending;
        private bool _active;

        public void Initialize()
        {
            _scroll.horizontalScroller.valueChanged += OnScroll;
            _scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        public void Show()
        {
            _active = true;
            Refresh();
        }

        public void Refresh()
        {
            if (_active == false || _pending != null) return;
            _pending = _scroll.schedule.Execute(UpdateViewport);
        }

        public void Hide()
        {
            _active = false;
            _pending?.Pause();
            _pending = null;
        }

        private void UpdateViewport()
        {
            _pending = null;
            if (_active == false || _scroll.panel == null) return;
            _pool.UpdateViewport(_scroll.contentViewport.worldBound);
        }

        private void OnScroll(float value) => Refresh();
        private void OnGeometry(GeometryChangedEvent value) => Refresh();

        public void Dispose()
        {
            Hide();
            _scroll.horizontalScroller.valueChanged -= OnScroll;
            _scroll.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _scroll.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        }
    }
}
