using Tanks.Core.Cosmetics;
using Tanks.UI.Menus.Rewards;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class StoreView : ViewBase<StoreViewModel>
    {
        [SerializeField] private RewardsView _rewardsView;

        private CosmeticCardPool _pool;
        private CosmeticCardViewport _viewport;
        private IVisualElementScheduledItem _restoreFocus;
        private VisualElement _root;
        private VisualElement _grid;
        private ScrollView _scroll;
        private VisualElement _catalogError;
        private VisualElement _catalogLoading;
        private VisualElement _bodyTab;
        private VisualElement _turretTab;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("StoreSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _grid = _root.Q("CosmeticGrid");
            _scroll = _grid.GetFirstAncestorOfType<ScrollView>();
            _scroll.focusable = false;
            _catalogError = _root.Q("CatalogError");
            _catalogLoading = _root.Q("CatalogLoading");
            _bodyTab = _root.Q("BodyTab");
            _turretTab = _root.Q("TurretTab");

            _root.Q<Button>("BackButton").clicked += () => ViewModel?.Back();
            _root.Q<Button>("BodyTab").clicked += () => SelectTab(CosmeticType.Body);
            _root.Q<Button>("TurretTab").clicked += () => SelectTab(CosmeticType.Turret);
            _root.Q<Button>("RetryCatalog").clicked += () => _ =
                ViewModel?.InitializeAsync();

            _grid.RegisterCallback<FocusInEvent>(OnCardFocused);
            _pool = new CosmeticCardPool(_grid,
                (id, currency) => ViewModel?.ConfirmPurchase(id, currency),
                id => _ = ViewModel?.EquipAsync(id),
                id => ViewModel?.PreviewCosmetic(id));

            _viewport = new CosmeticCardViewport(_scroll, _pool);
            _viewport.Initialize();
        }

        private void SelectTab(CosmeticType type)
        {
            _scroll.scrollOffset = Vector2.zero;
            ViewModel?.SelectTab(type);
        }

        protected override void Bind(StoreViewModel viewModel)
        {
            _scroll.scrollOffset = Vector2.zero;
            _viewport.Show();
            viewModel.Items.Bind(OnItemsChanged);
            viewModel.IsBusy.Bind(OnBusyChanged);
            viewModel.SelectedTab.Bind(OnTabChanged);
            viewModel.CatalogError.Bind(OnCatalogError);
            viewModel.IsCatalogLoading.Bind(OnCatalogLoading);
            _rewardsView.Show();
            _ = viewModel.InitializeAsync();
        }

        protected override void Unbind(StoreViewModel viewModel)
        {
            viewModel.Items.Unbind(OnItemsChanged);
            viewModel.IsBusy.Unbind(OnBusyChanged);
            viewModel.SelectedTab.Unbind(OnTabChanged);
            viewModel.CatalogError.Unbind(OnCatalogError);
            viewModel.IsCatalogLoading.Unbind(OnCatalogLoading);
            _rewardsView.Hide();
            _viewport.Hide();
            _restoreFocus?.Pause();
            _restoreFocus = null;
            _pool.Release();
        }

        protected override void Update()
        {
            base.Update();
            ViewModel?.Tick();
        }

        private void OnItemsChanged(CosmeticCardData[] items)
        {
            VisualElement focused = _grid.focusController?.focusedElement as VisualElement;
            string key = focused != null && _grid.Contains(focused) ?
                focused.viewDataKey : null;

            Vector2 offset = _scroll.scrollOffset;
            _pool.Bind(ViewModel);
            OnBusyChanged(ViewModel.IsBusy.Value);
            _viewport.Refresh();
            _restoreFocus?.Pause();
            _restoreFocus = _grid.schedule.Execute(() => RestoreFocus(key, offset));
        }

        private void RestoreFocus(string key, Vector2 offset)
        {
            _restoreFocus = null;
            if (ViewModel == null || _grid.panel == null) return;
            _scroll.scrollOffset = offset;
            VisualElement element = _pool.FindFocus(key);
            if (element == null || element.enabledInHierarchy == false) return;
            element.Focus();
        }

        private void OnCardFocused(FocusInEvent e)
        {
            if (e.target is VisualElement element) _scroll.ScrollTo(element);
        }

        private void OnBusyChanged(bool busy) => _grid.SetEnabled(busy == false);

        private void OnTabChanged(CosmeticType type)
        {
            _pool.Show(type);
            _viewport.Refresh();
            _bodyTab.EnableInClassList("selected-tab", type == CosmeticType.Body);
            _turretTab.EnableInClassList("selected-tab", type == CosmeticType.Turret);
        }

        private void OnCatalogError(bool error)
        {
            _catalogError.style.display = error ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnCatalogLoading(bool loading)
        {
            _catalogLoading.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void OnDestroy()
        {
            _restoreFocus?.Pause();
            _viewport?.Dispose();
            _pool?.Dispose();
        }
    }
}
