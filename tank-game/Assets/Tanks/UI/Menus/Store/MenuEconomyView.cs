using Tanks.Core.Players;
using Tanks.UI.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class MenuEconomyView : ViewBase<MenuEconomyViewModel>
    {
        private VisualElement _root;
        private BalanceElement _parts;
        private BalanceElement _pride;
        private bool _instant;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("MenuBalances");
            _parts = _root.Q<BalanceElement>("PartsBalance");
            _pride = _root.Q<BalanceElement>("PrideBalance");
        }

        protected override void Bind(MenuEconomyViewModel viewModel)
        {
            _instant = true;
            viewModel.Opacity.Bind(OnOpacity);
            viewModel.Balance.Bind(OnBalance);
            viewModel.PartsName.Bind(OnPartsName);
            viewModel.PartsDescription.Bind(OnPartsDescription);
            viewModel.PrideName.Bind(OnPrideName);
            viewModel.PrideDescription.Bind(OnPrideDescription);
        }

        protected override void Unbind(MenuEconomyViewModel viewModel)
        {
            viewModel.Opacity.Unbind(OnOpacity);
            viewModel.Balance.Unbind(OnBalance);
            viewModel.PartsName.Unbind(OnPartsName);
            viewModel.PartsDescription.Unbind(OnPartsDescription);
            viewModel.PrideName.Unbind(OnPrideName);
            viewModel.PrideDescription.Unbind(OnPrideDescription);
            _parts.ResetPresentation();
            _pride.ResetPresentation();
        }

        private void OnBalance(PlayerBalance balance)
        {
            _parts.SetValue(balance.SpareParts, _instant);
            _pride.SetValue(balance.PridePoints, _instant);
            _instant = false;
        }

        private void OnOpacity(float value)
        {
            _root.style.opacity = value;
            _root.pickingMode = value > 0 ? PickingMode.Position : PickingMode.Ignore;
            _parts.SetEnabled(value > 0);
            _pride.SetEnabled(value > 0);
            if (value > 0) return;
            _parts.ResetPresentation();
            _pride.ResetPresentation();
        }

        private void OnPartsName(string value) => _parts.CurrencyName = value;
        private void OnPartsDescription(string value) => _parts.Description = value;
        private void OnPrideName(string value) => _pride.CurrencyName = value;
        private void OnPrideDescription(string value) => _pride.Description = value;

        private void LateUpdate() => ViewModel?.Tick();

        private void OnApplicationFocus(bool focused)
        {
            if (focused && ViewModel != null) _ = ViewModel.RefreshAsync();
        }
    }
}
