using System.Collections.Generic;
using Tanks.Shared.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PlayerNames
{
    [DefaultExecutionOrder(1000)]
    public sealed class PlayerNamesView : ViewBase<PlayerNamesViewModel>
    {
        private VisualElement _root;
        private Label _spectatorName;
        private readonly Dictionary<IEntityBehaviour, Label> _labels = new();
        private readonly List<IEntityBehaviour> _removed = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlayerNames");
            _spectatorName = root.Q<Label>("SpectatorName");
        }

        protected override void Unbind(PlayerNamesViewModel viewModel)
        {
            _root.Clear();
            _labels.Clear();
            _spectatorName.text = "";
        }

        private void LateUpdate()
        {
            if (ViewModel == null || _root.panel == null || _root.resolvedStyle.display == DisplayStyle.None) return;
            if (_root.parent?.resolvedStyle.display == DisplayStyle.None) return;

            ViewModel.Tick();
            if (_spectatorName.text != ViewModel.SpectatorName) _spectatorName.text = ViewModel.SpectatorName;

            _removed.Clear();

            foreach (var pair in _labels)
            {
                if (ViewModel.Entries.ContainsKey(pair.Key) == false) _removed.Add(pair.Key);
            }

            foreach (IEntityBehaviour entity in _removed)
            {
                _labels[entity].RemoveFromHierarchy();
                _labels.Remove(entity);
            }

            foreach (var pair in ViewModel.Entries)
            {
                PlayerNameEntry entry = pair.Value;
                if (_labels.TryGetValue(pair.Key, out Label label) == false)
                {
                    if (entry.IsVisible == false) continue;

                    label = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
                    label.AddToClassList("player-name");
                    _root.Add(label);
                    _labels.Add(pair.Key, label);
                }

                DisplayStyle display = entry.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                if (label.style.display.value != display) label.style.display = display;
                if (entry.IsVisible == false) continue;

                if (label.text != entry.DisplayName) label.text = entry.DisplayName;

                Vector2 screen = Vector2.Scale(entry.ScreenPosition, new Vector2(Screen.width, Screen.height));
                Vector2 panel = RuntimePanelUtils.ScreenToPanel(_root.panel, screen);
                Vector2 local = _root.WorldToLocal(panel);
                label.style.left = local.x;
                label.style.top = local.y;
            }
        }
    }
}
