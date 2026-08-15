using Cysharp.Threading.Tasks;
using PlayFab;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tanks.Core;

namespace Tanks.Infrastructure
{
    public delegate void PlayFabCallback<TRequest, TResult>(
        TRequest request,
        Action<TResult> resultCallback,
        Action<PlayFabError> errorCallback,
        object customData = null,
        Dictionary<string, string> extraHeaders = null);

    public interface IPlayFabCaller
    {
        UniTask<ResultType<TResult>> CallAsync<TRequest, TResult>(
            PlayFabCallback<TRequest, TResult> playFabCallback,
            TRequest request,
            object customData = null,
            Dictionary<string, string> extraHeaders = null);

        string ErrorToCode<TEnum>(TEnum error) where TEnum : Enum;
    }

    public sealed class PlayFabCaller : IPlayFabCaller
    {
        public string ErrorToCode<TEnum>(TEnum error) where TEnum : Enum
        {
            return Regex.Replace(error.ToString(), "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
        }

        public async UniTask<ResultType<TResult>> CallAsync<TRequest, TResult>(
            PlayFabCallback<TRequest, TResult> playFabCallback,
            TRequest request,
            object customData = null,
            Dictionary<string, string> extraHeaders = null)
        {
            UniTaskCompletionSource<ResultType<TResult>> tcs = new();

            void ResultCallback(TResult result)
            {
                tcs.TrySetResult(Result.Success(result));
            }

            void ErrorCallback(PlayFabError error)
            {
                string errorCode = ErrorToCode(error.Error);
                tcs.TrySetResult(Result.Failure<TResult>(errorCode));
            }

            playFabCallback(request, ResultCallback, ErrorCallback, customData, extraHeaders);
            return await tcs.Task;
        }
    }
}