using System.Threading;
using System.Threading.Tasks;

namespace Tanks.Core.Common.Processes
{
    public interface IClientCommunicationService
    {
        Task<ResultType<TResponse>> SendRequestAsync<TPayload, TResponse>(
            TPayload message, CancellationToken cancellation = default);

        Task<ResultType> SendRequestAsync<T>(
            T message, CancellationToken cancellation = default);
    }
}
