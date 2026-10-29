using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Dialogs
{
    internal sealed class DialogFocusScope : IDisposable
    {
        private readonly VisualElement _dialog;
        private readonly VisualElement _panelRoot;
        private readonly VisualElement _previousFocus;
        private readonly Dictionary<VisualElement, bool> _focusability = new();
        private VisualElement _defaultControl;
        private IVisualElementScheduledItem _pendingFocus;
        private bool _disposed;

        public DialogFocusScope(VisualElement dialog)
        {
            _dialog = dialog;
            _panelRoot = dialog.panel.visualTree;
            _previousFocus = dialog.focusController.focusedElement as VisualElement;

            _panelRoot.Query<VisualElement>().ForEach(element =>
            {
                if (element == dialog || dialog.Contains(element)) return;
                _focusability.Add(element, element.focusable);
                element.focusable = false;
            });

            _panelRoot.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            _dialog.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        public void Focus(VisualElement control)
        {
            _defaultControl = control;
            _pendingFocus?.Pause();
            _pendingFocus = _dialog.schedule.Execute(() =>
            {
                if (_disposed == false && IsAvailable(_defaultControl)) _defaultControl.Focus();
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pendingFocus?.Pause();
            _panelRoot.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            _dialog.UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            foreach (var entry in _focusability) entry.Key.focusable = entry.Value;
            _focusability.Clear();

            if (IsAvailable(_previousFocus)) _previousFocus.Focus();
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (evt.target is not VisualElement element) return;
            if (element == _dialog || _dialog.Contains(element)) return;

            if (_focusability.ContainsKey(element) == false) _focusability.Add(element, element.focusable);
            element.focusable = false;
            evt.StopImmediatePropagation();
            element.Blur();
            Focus(_defaultControl);
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt) => Dispose();

        private static bool IsAvailable(VisualElement element)
        {
            if (element is null || element.panel is null || element.focusable == false
                || element.enabledInHierarchy == false) return false;

            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None
                    || current.resolvedStyle.visibility == Visibility.Hidden) return false;
            }

            return true;
        }
    }
}
