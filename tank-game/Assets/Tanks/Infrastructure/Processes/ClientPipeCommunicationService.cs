using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Processes;

namespace Tanks.Infrastructure.Processes
{
    public sealed class ClientPipeCommunicationService :
        PipeCommunicationServiceBase<NamedPipeClientStream>,
        IClientCommunicationService
    {
        public ClientPipeCommunicationService() :
            base(new NamedPipeClientStream(
                ".", "tank-game.pipe", PipeDirection.InOut, PipeOptions.Asynchronous))
        {

        }

        public async Task<ResultType<TResponse>> SendRequestAsync<TPayload, TResponse>(
            TPayload message, CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);
            var read = ReadAsync<TResponse>(cancellation);

            await SendAsync(message, cancellation);
            return await read;
        }

        public async Task<ResultType> SendRequestAsync<T>(
            T message, CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);
            return await SendAsync(message, cancellation);
        }

        protected override Task WaitForConnectionAsync(CancellationToken cancellation = default)
        {
            using CancellationTokenSource cts = GetCancellation(cancellation);
            return Pipe.ConnectAsync(cts.Token);
        }
    }
}
