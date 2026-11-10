using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public interface IStoreBackendService
    {
        Task<ResultType<CosmeticDefinitions>> GetDefinitionsAsync();
        Task<ResultType> PurchaseAsync(string identifier, string currency, int
            expectedPrice);

        Task<ResultType> EquipAsync(string identifier);
    }
}
