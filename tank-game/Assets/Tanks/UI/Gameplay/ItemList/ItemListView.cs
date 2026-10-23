using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.ItemList
{
    public sealed class ItemListView : ViewBase<ItemListViewModel>
    {
        private ItemListElement _root;
        private VisualElement _description;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q<ItemListElement>("ItemList");
            _description = root.Q("ItemDescriptionContainer");
            _description.RegisterCallback<GeometryChangedEvent>(OnDescriptionGeometryChanged);
        }

        protected override void Bind(ItemListViewModel viewModel)
        {
            viewModel.Entries.Bind(_root.SetEntries);
            UpdatePosition();
        }

        protected override void Unbind(ItemListViewModel viewModel)
        {
            viewModel.Entries.Unbind(_root.SetEntries);
            _root.Clear();
        }

        protected override void OnDestroy()
        {
            _description?.UnregisterCallback<GeometryChangedEvent>(OnDescriptionGeometryChanged);
            base.OnDestroy();
        }

        private void OnDescriptionGeometryChanged(GeometryChangedEvent evt) => UpdatePosition();

        private void UpdatePosition()
        {
            _root.style.top = _description.layout.yMax + 16;
        }
    }
}
