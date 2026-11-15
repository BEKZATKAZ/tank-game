using NUnit.Framework;
using Tanks.Shared.Teams;

namespace Tanks.Shared.Tests.Teams
{
    public sealed class PlayerStateTests
    {
        [Test]
        public void SetPing_UpdatesValueAndNotifiesOnlyWhenChanged()
        {
            PlayerState player = new PlayerState(1, "Player", new TeamId(1));
            PlayerState notified = null;
            int updates = 0;

            player.Updated += state =>
            {
                notified = state;
                updates++;
            };

            player.SetPing(42);
            player.SetPing(42);
            player.SetPing(0);

            Assert.That(player.PingMilliseconds, Is.Zero);
            Assert.That(updates, Is.EqualTo(2));
            Assert.That(notified, Is.SameAs(player));
        }
    }
}
