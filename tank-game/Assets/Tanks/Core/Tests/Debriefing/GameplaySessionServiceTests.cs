using NUnit.Framework;
using Tanks.Core.Common.Scenes;

namespace Tanks.Core.Tests.Debriefing
{
    public sealed class GameplaySessionServiceTests
    {
        [Test]
        public void DirectSceneClients_HaveDistinctLocalIdentities()
        {
            GameplaySessionService first = new();
            GameplaySessionService second = new();

            first.Begin(true, null);
            second.Begin(true, null);

            Assert.That(first.IsDirectSceneTest, Is.True);
            Assert.That(first.PlayerId, Is.Not.Empty);
            Assert.That(first.PlayerId, Is.Not.EqualTo(second.PlayerId));
        }

        [Test]
        public void NormalSession_PreservesAuthenticatedIdentityAndClearsErrors()
        {
            GameplaySessionService session = new();

            session.Begin(true, null);
            session.ConnectionError = "duplicate-player-id";
            session.Begin(false, "authenticated-player");

            Assert.That(session.IsDirectSceneTest, Is.False);
            Assert.That(session.PlayerId, Is.EqualTo("authenticated-player"));
            Assert.That(session.ConnectionError, Is.Null);
        }
    }
}
