using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public interface IStoreService
    {
        CosmeticDefinitions Definitions { get; }
        bool IsCatalogLoaded { get; }

        Task<ResultType> LoadCatalogAsync();
        Task<ResultType> RefreshCatalogAsync();
        Task<ResultType> PurchaseAsync(
            string identifier,
            string currency,
            int expectedPrice);

        Task<ResultType> EquipAsync(string identifier);
    }
}
