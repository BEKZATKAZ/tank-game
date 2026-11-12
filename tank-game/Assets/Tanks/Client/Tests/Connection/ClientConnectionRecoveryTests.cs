using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Connection;
using Tanks.Core.Common.Scenes;

namespace Tanks.Client.Tests.Connection
{
    public sealed class ClientConnectionRecoveryTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Disconnect_ReturnsToMenusOnceAndPreservesReason(bool loading)
        {
            ISceneLoader scenes = Substitute.For<ISceneLoader>();

            GameplaySessionService session = new();

            scenes.IsLoading.Returns(loading);

            ClientConnectionRecoveryService recovery = new(scenes, session);

            recovery.ReturnToMenus("duplicate-player-id");
            recovery.ReturnToMenus("connection-failed");

            Assert.That(session.ConnectionError, Is.EqualTo("duplicate-player-id"));

            scenes.Received(loading ? 1 : 0).EndLoading<LoadGameplay>();

            scenes.Received(1).StartLoading(Arg.Any<LoadMenus>(), withTips: false);
        }
    }
}
