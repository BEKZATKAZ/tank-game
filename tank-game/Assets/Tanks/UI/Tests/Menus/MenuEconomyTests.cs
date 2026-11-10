using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Localization;
using Tanks.UI.Menus.Sections;
using Tanks.UI.Menus.Store;
using Tanks.UI.Tests.Helpers;

namespace Tanks.UI.Tests.Menus
{
    public sealed class MenuEconomyTests
    {
        private ILocalPlayerService _player;
        private ILocalizationDatabase _localization;
        private LocalPlayer _data;
        private string _locale;

        [SetUp]
        public void Setup()
        {
            _locale = "en";
            _data = new LocalPlayer();
            _data.Balance.Value = new PlayerBalance { SpareParts = 1000 };
            _player = Substitute.For<ILocalPlayerService>();
            _player.LocalPlayer.Returns(_data);
            _player.IsLoaded.Returns(true);
            _localization = Substitute.For<ILocalizationDatabase>();
            _localization.TryGet("Menus", Arg.Any<string>(),
                out Arg.Any<ILocalizedString>()).Returns(call =>
            {
                call[2] = new TestLocalizedString(_locale + call.ArgAt<string>(1));
                return true;
            });
        }

        [Test]
        public void Economy_UpdatesBalancesWithoutOpeningStore()
        {
            using MenuEconomyViewModel model = new MenuEconomyViewModel(
                _player, _localization, Substitute.For<IMenuSectionService>());

            Assert.That(model.Balance.Value.SpareParts, Is.EqualTo(1000));

            _data.Apply(new PlayerSnapshot
            {
                Balance = new PlayerBalance { SpareParts = 1500, PridePoints = 250 }
            });
            Assert.That(model.Balance.Value.SpareParts, Is.EqualTo(1500));

            Assert.That(model.Balance.Value.PridePoints, Is.EqualTo(250));
        }

        [Test]
        public async Task Resume_CoalescesOverlappingPlayerRefreshes()
        {
            TaskCompletionSource<ResultType> pending = new();
            _player.LoadDataAsync().Returns(pending.Task);

            using MenuEconomyViewModel model = new MenuEconomyViewModel(
                _player, _localization, Substitute.For<IMenuSectionService>());

            Task first = model.RefreshAsync();
            await model.RefreshAsync();

            _ = _player.Received(1).LoadDataAsync();
            pending.SetResult(Result.Success());
            await first;
        }
        [Test]
        public void LocaleChanges_RefreshCurrencyNamesAndDescriptions()
        {
            using MenuEconomyViewModel model = new(
                _player, _localization, Substitute.For<IMenuSectionService>());

            _locale = "fr";
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>(_locale);

            Assert.That(model.PartsName.Value, Is.EqualTo(_locale + "currency.parts.name"));
            Assert.That(model.PrideName.Value, Is.EqualTo(_locale + "currency.pride.name"));
            Assert.That(model.PartsDescription.Value,
                Is.EqualTo(_locale + "currency.parts.description"));

            Assert.That(model.PrideDescription.Value,
                Is.EqualTo(_locale + "currency.pride.description"));
        }

        [Test]
        public void Disposal_UnsubscribesBalancesAndLocalizedText()
        {
            MenuEconomyViewModel model = new(
                _player, _localization, Substitute.For<IMenuSectionService>());

            PlayerBalance before = model.Balance.Value;
            string title = model.PartsName.Value;
            model.Dispose();
            _data.Apply(default);
            _locale = "fr";
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>(_locale);

            Assert.That(model.Balance.Value, Is.EqualTo(before));
            Assert.That(model.PartsName.Value, Is.EqualTo(title));
        }

        [TestCase(MenuSectionType.Main, 1)]
        [TestCase(MenuSectionType.Store, 1)]
        [TestCase(MenuSectionType.None, 0)]
        [TestCase(MenuSectionType.LobbyRoom, 0)]
        public void Navigation_ControlsBalanceVisibility(
            MenuSectionType section,
            int opacity)
        {
            IMenuSectionService navigation = Substitute.For<IMenuSectionService>();
            navigation.Current.Returns(section);
            using MenuEconomyViewModel model = new(_player, _localization, navigation);
            model.Tick();

            Assert.That(model.Opacity.Value, Is.EqualTo(opacity));
        }
    }
}
