using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using UnityEngine;

namespace Tanks.Infrastructure.Processes
{
    public abstract class PipeCommunicationServiceBase<TPipe> : IDisposable
        where TPipe : PipeStream
    {
        protected PipeCommunicationServiceBase(Func<TPipe> pipeCreation)
        {
            _pipeCreation = pipeCreation;
        }

        protected TPipe Pipe { get; private set; }
        protected CancellationTokenSource BaseCancellation { get; private set; }

        private readonly Func<TPipe> _pipeCreation;
        private StreamReader _reader;
        private StreamWriter _writer;

        public void Dispose()
        {
            BaseCancellation?.Cancel();
            Pipe?.Dispose();
            BaseCancellation?.Dispose();

            Pipe = null;
            BaseCancellation = null;
        }

        public void Reinitialize()
        {
            Dispose();

            Pipe = _pipeCreation();
            BaseCancellation = new CancellationTokenSource();

            _reader = new StreamReader(Pipe, new UTF8Encoding(false), true, 1024, leaveOpen: true);
            _writer = new StreamWriter(Pipe, new UTF8Encoding(false), 1024, leaveOpen: true);
        }

        public string ToJson<T>(T message)
        {
            string json = JsonConvert.SerializeObject(message);
            JObject obj = JObject.Parse(json);

            obj["$type"] = typeof(T).AssemblyQualifiedName;
            return obj.ToString(Formatting.None);
        }

        protected ResultType<T> FromJson<T>(string json)
        {
            try
            {
                JObject message = JObject.Parse(json);

                message.Remove("$type", out JToken typeToken);
                string typeString = typeToken?.Value<string>();

                Type type = Type.GetType(typeString);
                if (type == typeof(T)) return message.ToObject<T>();

                return Result.Failure<T>("unexpected-response-type");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<T>("failed-to-read-response");
            }
        }

        protected async Task<ResultType> SendAsync<T>(
            T message, CancellationToken cancellation = default)
        {
            try
            {
                using CancellationTokenSource cts = GetCancellation(cancellation);

                await Task.Run(() =>
                {
                    _writer.WriteLine(ToJson(message));
                    _writer.Flush();
                }, cancellationToken: cts.Token);

                return Result.Success();
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogException(ex);
                return Result.Failure("request-timed-out");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-send-request");
            }
        }

        protected async Task<ResultType<T>> ReadAsync<T>(
            CancellationToken cancellation = default)
        {
            try
            {
                using CancellationTokenSource cts = GetCancellation(cancellation);

                return await Task.Run(() =>
                {
                    string responseJson = _reader.ReadLine();
                    return FromJson<T>(responseJson);
                }, cancellationToken: cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogException(ex);
                return Result.Failure<T>("request-timed-out");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<T>("failed-to-read-response");
            }
        }

        protected CancellationTokenSource GetCancellation(CancellationToken local)
        {
            BaseCancellation ??= new CancellationTokenSource();
            return CancellationTokenSource.CreateLinkedTokenSource(local, BaseCancellation.Token);
        }

        protected abstract Task WaitForConnectionAsync(CancellationToken cancellation = default);
    }
}
