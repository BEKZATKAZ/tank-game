using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Store;
using Tanks.UI.Menus.Dialogs;
using Tanks.Utils;

namespace Tanks.UI.Menus.Store
{
    public sealed class StoreActionService : IDisposable
    {
        public StoreActionService(
            ILocalPlayerService player,
            IStoreService store,
            IDialogScreenService dialog)
        {
            _player = player;
            _store = store;
            _dialog = dialog;
        }

        public BindableProperty<bool> IsBusy { get; } = new();

        private readonly ILocalPlayerService _player;
        private readonly IStoreService _store;
        private readonly IDialogScreenService _dialog;
        private bool _disposed;

        public event Action Completed;

        public void ConfirmPurchase(
            IEnumerable<CosmeticCardData> items,
            string identifier,
            string currency)
        {
            if (IsBusy.Value || _disposed) return;

            foreach (CosmeticCardData item in items)
            {
                if (item.Config.Identifier != identifier || item.IsOwned) continue;

                foreach (PurchaseOption price in item.Prices)
                {
                    if (price.Currency != currency || price.CanAfford == false) continue;

                    _dialog.ShowPurchaseConfirmation(
                        item.Title, price.CurrencyName, price.Price,
                        () => _ = RunAsync(() => _store.PurchaseAsync(
                            identifier, currency, price.Price)));

                    return;
                }
            }
        }

        public Task EquipAsync(string identifier)
        {
            if (_player.LocalPlayer.Owns(identifier) == false) return Task.CompletedTask;
            return RunAsync(() => _store.EquipAsync(identifier));
        }

        private async Task RunAsync(Func<Task<ResultType>> action)
        {
            if (IsBusy.Value || _disposed || _player.IsLoaded == false) return;
            IsBusy.Value = true;

            try
            {
                ResultType result = await action();
                if (_disposed || result.IsSuccess) return;

                if (result.ErrorCode == "price-changed") await _store.RefreshCatalogAsync();
                if (result.ErrorCode != "price-changed") await _player.LoadDataAsync();
                if (_disposed == false) _dialog.ShowErrorFromCode("store",
                    result.ErrorCode);
            }
            finally
            {
                IsBusy.Value = false;
                if (_disposed == false) Completed?.Invoke();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Completed = null;
        }
    }
}
