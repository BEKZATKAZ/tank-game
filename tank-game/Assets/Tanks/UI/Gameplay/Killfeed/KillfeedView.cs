using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Killfeed
{
    public sealed class KillfeedView : ViewBase<KillfeedViewModel>
    {
        private VisualElement _root;
        private readonly Dictionary<KillfeedEntry, KillfeedElement> _elements = new();

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Killfeed");
            _root.pickingMode = PickingMode.Ignore;
        }

        protected override void Bind(KillfeedViewModel viewModel)
        {
            viewModel.Entries.Bind(UpdateEntries);
        }

        protected override void Unbind(KillfeedViewModel viewModel)
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

        private void UpdateEntries(IReadOnlyList<KillfeedEntry> entries)
        {
            HashSet<KillfeedEntry> activeEntries = new HashSet<KillfeedEntry>(entries);

            foreach (KillfeedEntry entry in new List<KillfeedEntry>(_elements.Keys))
            {
                if (activeEntries.Contains(entry)) continue;
                _elements[entry].RemoveFromHierarchy();
                _elements.Remove(entry);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                KillfeedEntry entry = entries[i];
                if (_elements.ContainsKey(entry)) continue;

                KillfeedElement element = new KillfeedElement();
                element.Set(entry);
                element.Play();
                _elements.Add(entry, element);
                _root.Insert(i, element);
            }
        }
    }
}
