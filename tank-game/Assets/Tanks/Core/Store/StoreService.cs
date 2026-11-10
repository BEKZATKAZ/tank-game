using System;
using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public sealed class StoreService : IStoreService, IDisposable
    {
        public StoreService(IStoreBackendService backend)
        {
            _backend = backend;
        }

        private readonly IStoreBackendService _backend;
        private Task<ResultType> _loading;
        private bool _disposed;

        public CosmeticDefinitions Definitions { get; private set; }
        public bool IsCatalogLoaded { get; private set; }

        public Task<ResultType> LoadCatalogAsync()
        {
            if (_disposed) return Task.FromResult(Result.Failure("cancelled"));
            if (_loading != null && _loading.IsCompleted == false) return _loading;
            _loading = null;
            if (IsCatalogLoaded) return Task.FromResult(Result.Success());

            _loading = LoadAsync();
            return _loading;
        }

        public Task<ResultType> RefreshCatalogAsync()
        {
            if (_loading != null && _loading.IsCompleted == false) return _loading;
            _loading = null;
            IsCatalogLoaded = false;
            return LoadCatalogAsync();
        }

        public Task<ResultType> PurchaseAsync(
            string identifier,
            string currency,
            int expectedPrice)
        {
            return _backend.PurchaseAsync(identifier, currency, expectedPrice);
        }

        public Task<ResultType> EquipAsync(string identifier)
        {
            return _backend.EquipAsync(identifier);
        }

        private async Task<ResultType> LoadAsync()
        {
            ResultType<CosmeticDefinitions> result = await _backend.GetDefinitionsAsync();
            if (_disposed) return Result.Failure("cancelled");
            if (result.IsSuccess == false) return Result.Failure(result.ErrorCode);

            Definitions = result.Value;
            IsCatalogLoaded = true;
            return Result.Success();
        }

        public void Dispose()
        {
            _disposed = true;
            IsCatalogLoaded = false;
            Definitions = default;
            _loading = null;
        }
    }
}
