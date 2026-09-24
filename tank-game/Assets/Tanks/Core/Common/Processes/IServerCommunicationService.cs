using System.Threading;
using System.Threading.Tasks;

namespace Tanks.Core.Common.Processes
{
    public interface IServerCommunicationService
    {
        Task<ResultType<TPayload>> ExpectRequestAsync<TPayload>(
            CancellationToken cancellation = default);

        Task<ResultType> RespondToRequestAsync<TResponse>(
            TResponse message, CancellationToken cancellation = default);
    }
}
