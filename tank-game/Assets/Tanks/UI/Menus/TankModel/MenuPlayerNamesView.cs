using System.Collections.Generic;
using Tanks.UI.Gameplay.PlayerNames;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.TankModel
{
    [DefaultExecutionOrder(1000)]
    public sealed class MenuPlayerNamesView : ViewBase<MenuPlayerNamesViewModel>
    {
        private VisualElement _root;
        private readonly List<Label> _labels = new();
        private readonly List<VisualElement> _readyChecks = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("MenuPlayerNames");
        }

        protected override void Unbind(MenuPlayerNamesViewModel viewModel)
        {
            _root.Clear();
            _labels.Clear();
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
                _labels[^1].RemoveFromHierarchy();
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

                    VisualElement check = new VisualElement { pickingMode = PickingMode.Ignore };
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
                    _root.Add(created);
                    _root.Add(check);
                    _labels.Add(created);
                    _readyChecks.Add(check);
                }

                PlayerNameEntry entry = ViewModel.Entries[i];
                Label label = _labels[i];
                VisualElement readyCheck = _readyChecks[i];
                readyCheck.style.display = entry.IsVisible && ViewModel.IsReady(i)
                    ? DisplayStyle.Flex : DisplayStyle.None;
                DisplayStyle display = entry.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;

                if (label.style.display.value != display) label.style.display = display;
                if (entry.IsVisible == false) continue;
                if (label.text != entry.DisplayName) label.text = entry.DisplayName;

                Vector2 local = ToLocalPosition(entry.ScreenPosition);
                label.style.left = local.x;
                label.style.top = local.y;

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
            Vector2 screen = Vector2.Scale(position, new Vector2(Screen.width, Screen.height));
            Vector2 panel = RuntimePanelUtils.ScreenToPanel(_root.panel, screen);
            return _root.WorldToLocal(panel);
        }
    }
}
