using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.UI.Menus.Extras;
using Tanks.UI.Menus.Items;
using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Tests.Menus
{
    public sealed class ItemBrowserTests : ViewModelTestBase<ItemBrowserViewModel>
    {
        private IItemCatalog _catalog;
        private ILocalizationDatabase _localization;
        private IMenuSectionService _navigation;
        private readonly Dictionary<string, string> _names = new();

        protected override ItemBrowserViewModel CreateViewModel()
        {
            _names.Clear();
            _catalog = Substitute.For<IItemCatalog>();
            _localization = Substitute.For<ILocalizationDatabase>();
            _navigation = Substitute.For<IMenuSectionService>();
            _localization.WaitForInitializationAsync().Returns(UniTask.CompletedTask);
            _localization.TryGet("Items", Arg.Any<string>(), out Arg.Any<ILocalizedString>())
                .Returns(call =>
                {
                    string key = call.ArgAt<string>(1);
                    if (_names.TryGetValue(key, out string value) == false) return false;

                    var text = Substitute.For<ILocalizedString>();
                    text.Get().Returns(value);
                    call[2] = text;
                    return true;
                });

            return new ItemBrowserViewModel(_catalog, _localization, _navigation);
        }

        private ItemCatalogEntry Entry(string id, ItemTier tier, string name)
        {
            var config = Substitute.For<IItemConfig>();
            var client = Substitute.For<IClientItemConfig>();
            config.Identifier.Returns(new ItemIdentifier(id));
            config.Tier.Returns(tier);
            config.ItemStats.Returns(new Dictionary<string, ItemStat>());
            config.PlayerStats.Returns(new Dictionary<StatType, PlayerStat>());
            _names[$"item.{id}.name"] = name;
            return new ItemCatalogEntry(config, client);
        }

        private void SetEntries(params ItemCatalogEntry[] entries)
        {
            _catalog.LoadAsync().Returns(UniTask.FromResult<IReadOnlyList<ItemCatalogEntry>>(entries));
        }

        [Test]
        public async Task LatestHighlight_ReplacesPreviousItemAndIgnoresItsLateLeave()
        {
            SetEntries(Entry("a", ItemTier.Rare, "Alpha"), Entry("b", ItemTier.Rare, "Beta"));
            await ViewModel.InitializeAsync(null);

            ViewModel.Hover(new ItemIdentifier("a"));
            ViewModel.Hover(new ItemIdentifier("b"));
            ViewModel.Leave(new ItemIdentifier("a"));
            Assert.AreEqual("Beta", ViewModel.HoveredItem.Value.Name);

            ViewModel.Hover(new ItemIdentifier("a"));
            Assert.AreEqual("Alpha", ViewModel.HoveredItem.Value.Name);
            ViewModel.Leave(new ItemIdentifier("a"));
            Assert.IsNull(ViewModel.HoveredItem.Value);
        }

        [Test]
        public async Task Initialize_SortsByTierThenLocalizedName()
        {
            SetEntries(Entry("epic", ItemTier.Epic, "Alpha"),
                Entry("rare-z", ItemTier.Rare, "Zulu"), Entry("rare-a", ItemTier.Rare, "Alpha"));

            await ViewModel.InitializeAsync(null);

            CollectionAssert.AreEqual(new[] { "rare-a", "rare-z", "epic" },
                ViewModel.Items.Value.Select(x => x.Config.Identifier.ToString()));
            Assert.IsNull(ViewModel.HoveredItem.Value);
        }

        [Test]
        public async Task HoverAndLeave_OnlyMatchingItemClearsDetails()
        {
            SetEntries(Entry("a", ItemTier.Rare, "Alpha"), Entry("b", ItemTier.Rare, "Beta"));
            await ViewModel.InitializeAsync(null);

            ViewModel.Hover(new ItemIdentifier("b"));
            ViewModel.Leave(new ItemIdentifier("a"));
            Assert.AreEqual("Beta", ViewModel.HoveredItem.Value.Name);

            ViewModel.Leave(new ItemIdentifier("b"));
            Assert.IsNull(ViewModel.HoveredItem.Value);
        }

        [Test]
        public async Task LocaleChange_ReordersAndRefreshesHoveredItem()
        {
            SetEntries(Entry("a", ItemTier.Rare, "Alpha"), Entry("b", ItemTier.Rare, "Beta"));
            await ViewModel.InitializeAsync(null);
            ViewModel.Hover(new ItemIdentifier("a"));

            _names["item.a.name"] = "Zulu";
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("fr");

            Assert.AreEqual("Beta", ViewModel.Items.Value[0].Name);
            Assert.AreEqual("Zulu", ViewModel.HoveredItem.Value.Name);
        }

        [Test]
        public async Task Dispose_DuringLoading_DoesNotPublishLateResults()
        {
            var completion = new UniTaskCompletionSource<IReadOnlyList<ItemCatalogEntry>>();
            _catalog.LoadAsync().Returns(completion.Task);
            var loading = ViewModel.InitializeAsync(null);

            ViewModel.Dispose();
            completion.TrySetResult(new[] { Entry("a", ItemTier.Rare, "Alpha") });
            await loading;
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("fr");

            Assert.IsEmpty(ViewModel.Items.Value);
            Assert.IsNull(ViewModel.HoveredItem.Value);
        }

        [Test]
        public async Task Dispose_DuringLocalization_DoesNotLoadCatalog()
        {
            var completion = new UniTaskCompletionSource();
            _localization.WaitForInitializationAsync().Returns(completion.Task);
            var loading = ViewModel.InitializeAsync(null);

            ViewModel.Dispose();
            completion.TrySetResult();
            await loading;

            _ = _catalog.DidNotReceive().LoadAsync();
        }

        [Test]
        public async Task EmptyCatalog_StaysEmpty()
        {
            SetEntries();
            await ViewModel.InitializeAsync(null);
            ViewModel.Hover(new ItemIdentifier("missing"));

            Assert.IsEmpty(ViewModel.Items.Value);
            Assert.IsNull(ViewModel.HoveredItem.Value);
        }

        [Test]
        public void Navigation_OpensItemsAndReturnsToExtras()
        {
            new ExtrasViewModel(_navigation).OpenItems();
            _navigation.Received().OpenSection(MenuSectionType.ItemBrowser);

            ViewModel.Back();
            _navigation.Received().OpenSection(MenuSectionType.Extras);
        }
    }
}
