using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Elements
{
    public sealed class ElementSpriteColorManipulator : Manipulator
    {
        private Color _border;
        private Color? _fill;
        private Color? _appliedBorder;
        private Color? _appliedFill;
        private IVisualElementScheduledItem _pending;
        private bool _hasColors;
        private bool _revealWhenReady;

        public void SetColors(Color border, Color? fill, bool revealWhenReady)
        {
            _border = border;
            _fill = fill;
            _hasColors = true;
            _revealWhenReady |= revealWhenReady;
            Apply();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<CustomStyleResolvedEvent>(OnStyle);
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            if (target.panel != null) Schedule();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<CustomStyleResolvedEvent>(OnStyle);
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            Cancel();
        }

        private void OnStyle(CustomStyleResolvedEvent value) => Apply();
        private void OnGeometryChanged(GeometryChangedEvent value) => Apply();
        private void OnAttach(AttachToPanelEvent value) => Schedule();

        private void OnDetach(DetachFromPanelEvent value)
        {
            Cancel();
            _appliedBorder = null;
            _appliedFill = null;
        }

        private void Schedule()
        {
            Cancel();
            _pending = target.schedule.Execute(() =>
            {
                _pending = null;
                Apply();
            });
        }

        private void Cancel()
        {
            _pending?.Pause();
            _pending = null;
        }

        private void Apply()
        {
            if (_hasColors == false || target.panel == null) return;
            if (_appliedBorder == _border && _appliedFill == _fill) return;
            MaterialDefinition material = target.resolvedStyle.unityMaterial;
            if (material.IsEmpty()) return;

            material.SetColor("_BorderColor", _border);
            if (_fill.HasValue) material.SetColor("_FillColor", _fill.Value);
            target.style.unityMaterial = material;
            if (_revealWhenReady) target.style.opacity = 1;
            _revealWhenReady = false;
            _appliedBorder = _border;
            _appliedFill = _fill;
        }
    }
}
