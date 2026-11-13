using System;
using System.Linq;
using Tanks.Client.Input.Bindings;
using Tanks.Client.Input.Glyphs;
using Tanks.UI.Menus.Settings.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings
{
    public sealed class ControlMappingsView
    {
        public void Initialize(VisualElement root)
        {
            _root = root.Q("ControlMappings");
            _database = Resources.Load<InputGlyphDatabase>("InputGlyphDatabase");
            _dialog.Initialize(root);
            _status = root.Q<Label>("BindingStatus");
            VisualElement headers = new();

            headers.AddToClassList("control-row");
            Label title = new();

            title.AddToClassList("control-row__label");
            BindingLabels.Bind(title, "text", "control-label");
            headers.Add(title);

            foreach (string column in new[]
                { "keyboard.0", "keyboard.1", "gamepad.0", "gamepad.1" })
            {
                Label label = new();

                label.AddToClassList("control-column-header");
                BindingLabels.Bind(label, "text", "column." + column);
                headers.Add(label);
            }

            VisualElement clearHeader = new();

            clearHeader.AddToClassList("control-row__clear");
            headers.Add(clearHeader);
            _root.Add(headers);

            foreach (string row in _rows)
                _root.Add(new ControlBindingRow(row, Capture, Clear));

            _fields = _root.Query<BindingField>().ToList().ToArray();
            root.RegisterCallback<KeyDownEvent>(Suppress, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(
                Suppress, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(
                Suppress, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(
                Suppress, TrickleDown.TrickleDown);
        }

        private static readonly string[] _rows =
        {
            "shoot", "modern.up", "modern.down", "modern.left", "modern.right",
            "modern.handbrake", "modern.reverse", "classic.forward",
            "classic.backwards", "classic.turn-left", "classic.turn-right"
        };

        private readonly BindingCaptureDialogView _dialog = new();

        private VisualElement _root;
        private InputGlyphDatabase _database;
        private BindingField[] _fields;
        private BindingCaptureDialogViewModel _capture;
        private Label _status;
        private IControlBindingService _bindings;
        private Action<string> _clear;

        public void Bind(
            IControlBindingService bindings,
            BindingCaptureDialogViewModel capture,
            Action<string> clear)
        {
            _bindings = bindings;
            _capture = capture;
            _clear = clear;
            _dialog.Bind(capture);
            _bindings.Changed += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            if (_bindings == null) return;

            _dialog.Unbind();
            _bindings.Changed -= Refresh;
            _bindings.CancelCapture();
            _bindings = null;
            _capture = null;
            _clear = null;
        }

        private void Clear(string row) => _clear?.Invoke(row);
        private void Capture(string slot) => _capture?.Capture(slot);

        private void Suppress(EventBase evt)
        {
            if (_dialog.IsBlockingNavigation) evt.StopImmediatePropagation();
        }

        private void Refresh()
        {
            foreach (BindingField field in _fields)
            {
                field.Refresh(_database, _bindings.GetPath(field.Slot),
                    _bindings.CaptureSlot == field.Slot);
            }

            bool statusVisible = _bindings.Error != null;

            BindingLabels.Bind(_status, "text", "save-failed");
            _status.style.display = statusVisible
                ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
