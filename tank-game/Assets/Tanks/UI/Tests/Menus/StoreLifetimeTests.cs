using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Cosmetics;
using Tanks.Localization;
using Tanks.UI.Menus.Store;

namespace Tanks.UI.Tests.Menus
{
    public sealed class StoreLifetimeTests : StoreTestsBase
    {
        [Test]
        public void DisposedStore_IgnoresPlayerLocaleTimerAndTabChanges()
        {
            CosmeticCardData[] items = _model.Items.Value;
            int revision = items[0].Revision;
            CosmeticType tab = _model.SelectedTab.Value;
            _store.ClearReceivedCalls();
            _model.Dispose();

            _now = _now.AddDays(1);
            _data.Revision.Value++;
            _localization.LocaleChanged += Raise.Event<LocaleChangedEventHandler>("fr");
            _model.Tick();
            _model.SelectTab(CosmeticType.Turret);

            Assert.That(_model.Items.Value, Is.SameAs(items));
            Assert.That(items[0].Revision, Is.EqualTo(revision));
            Assert.That(_model.SelectedTab.Value, Is.EqualTo(tab));

            _ = _store.DidNotReceive().LoadCatalogAsync();
        }

        [Test]
        public async Task DisposedStore_IgnoresPendingCatalogCompletion()
        {
            TaskCompletionSource<ResultType> pending = new();
            _store.LoadCatalogAsync().Returns(pending.Task);
            CosmeticCardData[] items = _model.Items.Value;
            int revision = items[0].Revision;
            Task loading = _model.InitializeAsync();
            _model.Dispose();
            pending.SetResult(Result.Success());
            await loading;

            Assert.That(_model.Items.Value, Is.SameAs(items));
            Assert.That(items[0].Revision, Is.EqualTo(revision));
            Assert.That(_model.IsCatalogLoading.Value, Is.False);
        }

        [Test]
        public async Task DisposedStore_DoesNotRefreshOrShowLateMutationErrors()
        {
            _data.Cosmetics.Value.Add(_body.Identifier);
            TaskCompletionSource<ResultType> pending = new();
            _store.EquipAsync(_body.Identifier).Returns(pending.Task);
            Task equipping = _model.EquipAsync(_body.Identifier);
            _model.Dispose();
            pending.SetResult(Result.Failure("conflict"));
            await equipping;

            _ = _player.DidNotReceive().LoadDataAsync();
            _dialog.DidNotReceiveWithAnyArgs().ShowErrorFromCode(default, default);
            Assert.That(_model.IsBusy.Value, Is.False);
        }
    }
}
