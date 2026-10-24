using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.PickupFeed
{
    public sealed class PickupFeedView : ViewBase<PickupFeedViewModel>
    {
        private VisualElement _root;
        private readonly Dictionary<PickupFeedEntry, PickupFeedElement> _elements = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PickupFeed");
            _root.pickingMode = PickingMode.Ignore;
        }

        protected override void Bind(PickupFeedViewModel viewModel)
        {
            viewModel.Entries.Bind(UpdateEntries);
        }

        protected override void Unbind(PickupFeedViewModel viewModel)
        {
            viewModel.Entries.Unbind(UpdateEntries);

            _root.Clear();
            _elements.Clear();
        }

        protected override void Update()
        {
            base.Update();
            ViewModel?.Tick(Time.unscaledDeltaTime);
        }

        private void UpdateEntries(IReadOnlyList<PickupFeedEntry> entries)
        {
            HashSet<PickupFeedEntry> activeEntries = new HashSet<PickupFeedEntry>(entries);

            foreach (PickupFeedEntry entry in new List<PickupFeedEntry>(_elements.Keys))
            {
                if (activeEntries.Contains(entry)) continue;

                _elements[entry].RemoveFromHierarchy();
                _elements.Remove(entry);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                PickupFeedEntry entry = entries[i];
                if (_elements.ContainsKey(entry)) continue;

                PickupFeedElement element = new PickupFeedElement();
                element.Set(entry);
                element.Play();

                _elements.Add(entry, element);
                _root.Insert(i, element);
            }
        }
    }
}
