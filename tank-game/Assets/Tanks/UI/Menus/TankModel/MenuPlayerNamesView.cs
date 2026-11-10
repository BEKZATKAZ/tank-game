using System.Collections.Generic;
using Tanks.UI.Gameplay.PlayerNames;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.TankModel
{
    [DefaultExecutionOrder(1000)]
    public sealed class MenuPlayerNamesView : ViewBase<MenuPlayerNamesViewModel>
    {
        private readonly List<Label> _labels = new();
        private readonly List<Label> _levels = new();
        private readonly List<VisualElement> _nameRows = new();
        private readonly List<VisualElement> _readyChecks = new();
        private VisualElement _root;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("MenuPlayerNames");
        }

        protected override void Unbind(MenuPlayerNamesViewModel viewModel)
        {
            _root.Clear();
            _labels.Clear();
            _levels.Clear();
            _nameRows.Clear();
            _readyChecks.Clear();
        }

        private void LateUpdate()
        {
            if (ViewModel == null ||
                _root.panel == null ||
                _root.resolvedStyle.display == DisplayStyle.None) return;

            ViewModel.Tick();

            while (_labels.Count > ViewModel.Entries.Count)
            {
                _nameRows[^1].RemoveFromHierarchy();
                _nameRows.RemoveAt(_nameRows.Count - 1);
                _levels.RemoveAt(_levels.Count - 1);
                _labels.RemoveAt(_labels.Count - 1);
                _readyChecks[^1].RemoveFromHierarchy();
                _readyChecks.RemoveAt(_readyChecks.Count - 1);
            }

            for (int i = 0; i < ViewModel.Entries.Count; i++)
            {
                if (i == _labels.Count)
                {
                    Label created = new Label()
                    {
                        pickingMode = PickingMode.Ignore,
                        enableRichText = false
                    };

                    VisualElement check = new() { pickingMode = PickingMode.Ignore };
                    check.AddToClassList("player-ready-check");
                    check.generateVisualContent += context =>
                    {
                        Painter2D painter = context.painter2D;
                        painter.strokeColor = Color.red;
                        painter.lineWidth = 8;
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(2, 13));
                        painter.LineTo(new Vector2(9, 20));
                        painter.LineTo(new Vector2(24, 4));
                        painter.Stroke();
                    };

                    created.AddToClassList("player-name");
                    _root.Add(check);
                    Label createdLevel = new Label { pickingMode = PickingMode.Ignore };
                    createdLevel.AddToClassList("menu-player-level");
                    VisualElement row = new() { pickingMode = PickingMode.Ignore };
                    row.AddToClassList("menu-player-name-row");
                    row.Add(createdLevel);
                    row.Add(created);
                    _root.Add(row);
                    _nameRows.Add(row);
                    _levels.Add(createdLevel);
                    _labels.Add(created);
                    _readyChecks.Add(check);
                }

                PlayerNameEntry entry = ViewModel.Entries[i];
                Label label = _labels[i];
                VisualElement readyCheck = _readyChecks[i];
                readyCheck.style.display = entry.IsVisible && ViewModel.IsReady(i)
                    ? DisplayStyle.Flex : DisplayStyle.None;
                DisplayStyle display = entry.IsVisible
                    ? DisplayStyle.Flex : DisplayStyle.None;

                VisualElement nameRow = _nameRows[i];
                if (nameRow.style.display.value != display) nameRow.style.display = display;
                if (entry.IsVisible == false) continue;
                if (label.text != entry.DisplayName) label.text = entry.DisplayName;

                Vector2 local = ToLocalPosition(entry.ScreenPosition);
                nameRow.style.left = local.x;
                nameRow.style.top = local.y;
                Label level = _levels[i];
                level.text = ViewModel.GetLevel(i).ToString();

                if (ViewModel.IsReady(i))
                {
                    Vector2 center = ToLocalPosition(ViewModel.GetTankScreenPosition(i));
                    readyCheck.style.left = center.x;
                    readyCheck.style.top = center.y;
                }
            }
        }

        private Vector2 ToLocalPosition(Vector2 position)
        {
            Vector2 screen = Vector2.Scale(position,
                new Vector2(Screen.width, Screen.height));

            Vector2 panel = RuntimePanelUtils.ScreenToPanel(_root.panel, screen);
            return _root.WorldToLocal(panel);
        }
    }
}
