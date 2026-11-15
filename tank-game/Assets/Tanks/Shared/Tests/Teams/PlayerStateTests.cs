using NUnit.Framework;
using Tanks.Shared.Teams;

namespace Tanks.Shared.Tests.Teams
{
    public sealed class PlayerStateTests
    {
        [Test]
        public void SetEliminated_NotifiesOnlyOnStateChanges()
        {
            PlayerState player = new PlayerState(1, "Player", new TeamId(1));
            int updates = 0;
            bool eliminated = false;

            player.Updated += state =>
            {
                updates++;
                eliminated = state.IsEliminated;
            };

            player.SetEliminated(false);

            Assert.That(updates, Is.Zero);

            PlayerState result = player.SetEliminated(true);

            Assert.That(result, Is.SameAs(player));
            Assert.That(eliminated, Is.True);

            player.SetEliminated(true);
            player.SetEliminated(false);

            Assert.That(updates, Is.EqualTo(2));
            Assert.That(eliminated, Is.False);
            Assert.That(player.IsEliminated, Is.False);
        }

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
