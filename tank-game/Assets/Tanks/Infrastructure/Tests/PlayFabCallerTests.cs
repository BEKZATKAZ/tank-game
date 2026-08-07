using NUnit.Framework;
using PlayFab;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Tanks.Infrastructure.Tests
{
    public class PlayFabCallerTests
    {
        private IPlayFabCaller _caller;

        [SetUp]
        public void Setup()
        {
            _caller = new PlayFabCaller();
        }

        [Test]
        public async Task CallAsync_Success_RunsSuccessCallback()
        {
            static void Method<TRequest>(TRequest request, Action<string> resultCallback,
                Action<PlayFabError> errorCallback,
                object customData = null,
                Dictionary<string, string> extraHeaders = null)
            {
                resultCallback("result");
            }

            ResultType<string> result = await _caller
                .CallAsync<string, string>(Method, "request");

            Assert.True(result.IsSuccess);
            Assert.AreEqual("result", result.Value);
        }

        [TestCase(404, PlayFabErrorCode.CloudScriptNotFound, "cloud-script-not-found")]
        [TestCase(500, PlayFabErrorCode.InternalServerError, "internal-server-error")]
        [TestCase(400, PlayFabErrorCode.LobbyBadRequest, "lobby-bad-request")]
        public async Task CallAsync_Success_RunsSuccessCallback(
            int httpCode, PlayFabErrorCode error, string expectedErrorCode)
        {
            async void Method<TRequest>(TRequest request, Action<string> resultCallback,
                Action<PlayFabError> errorCallback,
                object customData = null,
                Dictionary<string, string> extraHeaders = null)
            {
                await Task.Delay(100);

                errorCallback(new PlayFabError
                {
                    Error = error,
                    HttpCode = httpCode
                });
            }

            ResultType<string> result = await _caller
                .CallAsync<string, string>(Method, "request");

            Assert.False(result.IsSuccess);
            Assert.AreEqual(expectedErrorCode, result.ErrorCode);
        }
    }
}
