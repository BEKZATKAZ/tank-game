using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common.Processes;
using Tanks.Infrastructure.Processes;
using UnityEngine;

namespace Tanks.Infrastructure.Tests
{
    [Serializable]
    public struct TestData
    {
        public string Value1;
        public int Value2;
    }

    public class PipeCommunicationTests
    {
        private const string PROCESS_PATH = "../native/out/build/x64-release/pipetest/pipetest.exe";

        private IProcessService _service;
        private ClientPipeCommunicationService _pipe;
        private int _lastProcessId;

        [SetUp]
        public void Setup()
        {
            _service = new ProcessService();
            _pipe = new ClientPipeCommunicationService();
            _lastProcessId = -1;
        }

        [TearDown]
        public void TearDown()
        {
            if (_lastProcessId != -1) _service.KillById(_lastProcessId);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task SendRequest_WithResponse_WaitsForResponse(bool standalone)
        {
            if (File.Exists(PROCESS_PATH) == false)
            {
                Debug.LogWarning("A pipetest build does not exist, skipping test...");
                return;
            }

            TestData dataToSend = new TestData { Value1 = "send", Value2 = 2 };
            TestData dataToRead = new TestData { Value1 = "read", Value2 = 3 };

            static void OnStderr(string error) => Assert.Fail("stderr: " + error);
            static void OnStdout(string data) => Debug.Log("stdout: " + data);

            _lastProcessId = _service.Launch(PROCESS_PATH,
                "--pipe-name tank-game.pipe " +
                $"--reads \"{_pipe.ToJson(dataToSend).Replace("\"", "\\\"")}\" " +
                $"--sends \"{_pipe.ToJson(dataToRead).Replace("\"", "\\\"")}\"",

                stdout: standalone ? null : OnStdout,
                stderr: standalone ? null : OnStderr);

            _pipe.Reinitialize();
            using CancellationTokenSource timeout = new CancellationTokenSource(10000);

            ResultType<TestData> response = await _pipe
                .SendRequestAsync<TestData, TestData>(dataToSend, timeout.Token);

            Assert.True(response.IsSuccess, $"Request failed: {response.ErrorCode}");
            CompareData(dataToRead, response.Value);
        }

        [Test]
        public async Task SendRequest_WithoutResponse_OnlyWaitsForSend()
        {
            if (File.Exists(PROCESS_PATH) == false)
            {
                Debug.LogWarning("A pipetest build does not exist, skipping test...");
                return;
            }

            TestData dataToSend = new TestData { Value1 = "send", Value2 = 2 };
            TestData dataToRead = new TestData { Value1 = "read", Value2 = 3 };

            _lastProcessId = _service.Launch(PROCESS_PATH,
                "--pipe-name tank-game.pipe " +
                $"--reads \"{_pipe.ToJson(dataToSend).Replace("\"", "\\\"")}\"");

            _pipe.Reinitialize();
            using CancellationTokenSource timeout = new CancellationTokenSource(10000);

            ResultType response = await _pipe.SendRequestAsync(dataToSend, timeout.Token);
            Assert.True(response.IsSuccess, $"Request failed: {response.ErrorCode}");

            _pipe.Reinitialize();
        }

        private void CompareData(TestData expected, TestData actual)
        {
            Assert.AreEqual(expected.Value1, actual.Value1);
            Assert.AreEqual(expected.Value2, actual.Value2);
        }
    }
}
