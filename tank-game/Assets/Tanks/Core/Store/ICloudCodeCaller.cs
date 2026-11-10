using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public interface ICloudCodeCaller
    {
        Task<ResultType> CallAsync<TPayload>(TPayload payload);
        Task<ResultType<TResponse>> CallAsync<TPayload, TResponse>(TPayload payload);
    }
}
