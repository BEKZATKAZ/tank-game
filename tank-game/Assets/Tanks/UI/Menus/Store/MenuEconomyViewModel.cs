using System;
using System.Threading.Tasks;
using Tanks.Core.Players;
using Tanks.Localization;
using Tanks.UI.Menus.Sections;
using Tanks.Utils;

namespace Tanks.UI.Menus.Store
{
    public sealed class MenuEconomyViewModel : IViewModel, IDisposable
    {
        public MenuEconomyViewModel(
            ILocalPlayerService player,
            ILocalizationDatabase localization,
            IMenuSectionService navigation)
        {
            _player = player;
            _localization = localization;
            _navigation = navigation;

            _player.LocalPlayer.Revision.Bind(OnPlayerChanged);
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public BindableProperty<PlayerBalance> Balance { get; } = new();
        public BindableProperty<float> Opacity { get; } = new();
        public BindableProperty<string> PartsName { get; } = new();
        public BindableProperty<string> PartsDescription { get; } = new();
        public BindableProperty<string> PrideName { get; } = new();
        public BindableProperty<string> PrideDescription { get; } = new();

        private readonly ILocalPlayerService _player;
        private readonly ILocalizationDatabase _localization;
        private readonly IMenuSectionService _navigation;
        private bool _refreshing;

        public async Task RefreshAsync()
        {
            if (_refreshing || _player.IsLoaded == false) return;
            _refreshing = true;

            try { await _player.LoadDataAsync(); }
            finally { _refreshing = false; }
        }

        public void Tick()
        {
            Opacity.Value = _navigation.Current is
                MenuSectionType.None or MenuSectionType.LobbyRoom
                ? 0 : 1;
        }

        private void OnPlayerChanged(int revision)
        {
            Balance.Value = _player.LocalPlayer.Balance.Value;
            RefreshText();
            Tick();
        }

        private void OnLocaleChanged(string locale) => RefreshText();

        private void RefreshText()
        {
            PartsName.Value = GetText("currency.parts.name");
            PartsDescription.Value = GetText("currency.parts.description");
            PrideName.Value = GetText("currency.pride.name");
            PrideDescription.Value = GetText("currency.pride.description");
        }

        private string GetText(string key)
        {
            return _localization.TryGet("Menus", key, out ILocalizedString value)
                ? value.Get() : "";
        }

        public void Dispose()
        {
            _player.LocalPlayer.Revision.Unbind(OnPlayerChanged);
            _localization.LocaleChanged -= OnLocaleChanged;
        }
    }
}
