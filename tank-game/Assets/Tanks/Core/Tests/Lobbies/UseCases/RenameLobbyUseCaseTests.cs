using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.UseCases;
using UnityEngine;

namespace Tanks.Core.Tests.Lobbies.UseCases
{
    public class RenameLobbyUseCaseTests
    {
        private RenameLobbyUseCase _useCase;
        private ILobbyService _service;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<ILobbyService>();
            _useCase = new RenameLobbyUseCase(_service);
        }

        [TestCase("new name")]
        [TestCase("someone's lobby")]
        [TestCase("test")]
        public async Task Update_OnlyUpdatesName(string newName)
        {
            _service
                .UpdateLobbyAsync(Arg.Is<LobbyUpdateRequest>(
                    x => x.Name == newName &&
                    x.JoinCode == null &&
                    x.Status == null))
                .Returns(UniTask.FromResult(Result.Success<LobbyState>(null)));

            ResultType result = await _useCase.RenameAsync(newName);
            Assert.True(result.IsSuccess);
        }
    }
}
