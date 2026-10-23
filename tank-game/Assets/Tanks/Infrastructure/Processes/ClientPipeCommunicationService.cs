using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using UnityEngine;

namespace Tanks.Infrastructure.Processes
{
    public sealed class ClientPipeCommunicationService :
        PipeCommunicationServiceBase<NamedPipeClientStream>,
        IClientCommunicationService
    {
        public ClientPipeCommunicationService() :
            base(() => new NamedPipeClientStream(
                ".", "tank-game.pipe", PipeDirection.InOut, PipeOptions.Asynchronous))
        {

        }

        public async Task<ResultType<TResponse>> SendRequestAsync<TPayload, TResponse>(
            TPayload message, CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);

            Debug.LogFormat(
                "Sending request of type '{0}' and waiting response of type",
                typeof(TPayload).Name, typeof(TResponse).Name);

            var read = ReadAsync<TResponse>(cancellation);

            await SendAsync(message, cancellation);
            return await read;
        }

        public async Task<ResultType> SendRequestAsync<T>(
            T message, CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);
            Debug.LogFormat("Sending request of type '{0}'", typeof(T).Name);
            return await SendAsync(message, cancellation);
        }

        protected override Task WaitForConnectionAsync(CancellationToken cancellation = default)
        {
            Debug.Log("Waiting for connection");
            using CancellationTokenSource cts = GetCancellation(cancellation);
            return Pipe.ConnectAsync(cts.Token);
        }
    }
}
