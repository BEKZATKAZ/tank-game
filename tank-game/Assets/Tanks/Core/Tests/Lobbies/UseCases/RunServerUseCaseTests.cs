using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using Tanks.Core.Lobbies.UseCases;
using UnityEngine;

namespace Tanks.Core.Tests.Lobbies.UseCases
{
    public class RunServerUseCaseTests
    {
        private IAuthService _auth;
        private IProcessService _service;
        private IClientCommunicationService _client;
        private RunServerUseCase _useCase;

        [SetUp]
        public void Setup()
        {
            _auth = Substitute.For<IAuthService>();
            _service = Substitute.For<IProcessService>();
            _client = Substitute.For<IClientCommunicationService>();
            _useCase = new RunServerUseCase(_auth, _service, _client);
        }

        [Test]
        public void RunAsync_RemovesDuplicates()
        {
            _client
                .SendRequestAsync<AuthenticateServerRequest, ServerAuthReady>(
                    Arg.Any<AuthenticateServerRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(new ServerAuthReady { AuthCode = "test" })));

            _service.Launch(Arg.Any<string>(), Arg.Any<string>()).Returns(100);
            _useCase.RunAsync().Forget();

            _service.Launch(Arg.Any<string>(), Arg.Any<string>()).Returns(101);
            _useCase.RunAsync().Forget();

            _service.Received(1).KillById(100);
        }

        [Test]
        public async Task RunAsync_StartsServer_Authenticates()
        {
            _client
                .SendRequestAsync<AuthenticateServerRequest, ServerAuthReady>(
                    Arg.Any<AuthenticateServerRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success(new ServerAuthReady { AuthCode = "test" })));

            _auth
                .ConfirmCodeAsync("test")
                .Returns(UniTask.FromResult(Result.Success()));

            ResultType result = await _useCase.RunAsync();
            Assert.True(result.IsSuccess);

            _service
                .Received(1)
                .Launch(RunServerUseCase.SERVER_PATH, RunServerUseCase.SERVER_ARGUMENTS);

            _auth.Received(1).ConfirmCodeAsync("test").Forget();
        }

        [Test]
        public async Task RunAsync_FailsToAuthenticate_KillsServerProcess()
        {
            _client
                .SendRequestAsync<AuthenticateServerRequest, ServerAuthReady>(
                    Arg.Any<AuthenticateServerRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Failure<ServerAuthReady>("error")));

            _service.Launch(Arg.Any<string>(), Arg.Any<string>()).Returns(105);
            ResultType result = await _useCase.RunAsync();

            Assert.False(result.IsSuccess);
            Assert.AreEqual("error", result.ErrorCode);

            _service.Received(1).KillById(105);
            _auth.DidNotReceiveWithAnyArgs().ConfirmCodeAsync(Arg.Any<string>()).Forget();
        }
    }
}
