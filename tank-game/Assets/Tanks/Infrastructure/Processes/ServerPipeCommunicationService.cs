using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using UnityEngine;

namespace Tanks.Infrastructure.Processes
{
    public sealed class ServerPipeCommunicationService :
        PipeCommunicationServiceBase<NamedPipeServerStream>,
        IServerCommunicationService
    {
        public ServerPipeCommunicationService() :
            base(new NamedPipeServerStream(
                "tank-game.pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte))
        { }

        public async Task<ResultType<TPayload>> ExpectRequestAsync<TPayload>(
            CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);
            Debug.LogFormat("Expecting request of type '{0}'", typeof(TPayload).Name);
            return await ReadAsync<TPayload>(cancellation);
        }

        public async Task<ResultType> RespondToRequestAsync<TResponse>(
            TResponse message, CancellationToken cancellation = default)
        {
            if (Pipe.IsConnected == false) await WaitForConnectionAsync(cancellation);
            Debug.LogFormat("Responding of type '{0}'", typeof(TResponse).Name);
            return await SendAsync(message, cancellation);
        }

        protected override Task WaitForConnectionAsync(CancellationToken cancellation = default)
        {
            Debug.Log("Waiting for connection");
            using CancellationTokenSource cts = GetCancellation(cancellation);
            return Pipe.WaitForConnectionAsync(cts.Token);
        }
    }
}
